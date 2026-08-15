using System.Security.Claims;
using HRConnect.Application.DTOs;
using HRConnect.Domain.Entities;
using HRConnect.Domain.Enums;
using HRConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRConnect.API.Controllers;

[ApiController]
[Authorize]
[Route("api/leave")]
public sealed class LeaveController : ControllerBase
{
    private readonly AppDbContext _db;
    public LeaveController(AppDbContext db) => _db = db;

    [HttpGet("types")]
    public async Task<IActionResult> GetTypes([FromQuery] bool includeInactive = false)
    {
        var query = _db.LeaveTypes.AsNoTracking();
        if (!includeInactive) query = query.Where(x => x.IsActive);
        return Ok(await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync());
    }

    [Authorize(Roles = "HR,Admin")]
    [HttpPost("types")]
    public async Task<IActionResult> CreateType(LeaveTypeWriteDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        var name = dto.Name.Trim();
        if (await _db.LeaveTypes.AnyAsync(x => x.Code == code || x.Name == name))
            return Conflict(new { message = "A leave type with this code or name already exists." });
        var leaveType = new LeaveType(); ApplyTypeDto(leaveType, dto);
        _db.LeaveTypes.Add(leaveType); await _db.SaveChangesAsync();
        return Created(string.Empty, leaveType);
    }

    [Authorize(Roles = "HR,Admin")]
    [HttpPut("types/{id:int}")]
    public async Task<IActionResult> UpdateType(int id, LeaveTypeWriteDto dto)
    {
        var leaveType = await _db.LeaveTypes.FindAsync(id);
        if (leaveType == null) return NotFound();
        var code = dto.Code.Trim().ToUpperInvariant(); var name = dto.Name.Trim();
        if (await _db.LeaveTypes.AnyAsync(x => x.Id != id && (x.Code == code || x.Name == name)))
            return Conflict(new { message = "A leave type with this code or name already exists." });
        ApplyTypeDto(leaveType, dto); await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances([FromQuery] int? employeeId, [FromQuery] int? year)
    {
        var account = await CurrentAccountAsync();
        if (account == null) return Unauthorized();
        var targetEmployeeId = employeeId ?? account.EmployeeId;
        if (!targetEmployeeId.HasValue) return BadRequest(new { message = "Your user account is not linked to an employee profile." });
        if (!await CanAccessEmployeeAsync(account, targetEmployeeId.Value)) return Forbid();

        var targetYear = year ?? DateTime.UtcNow.Year;
        var types = await _db.LeaveTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync();
        var balances = await _db.LeaveBalances.AsNoTracking()
            .Where(x => x.EmployeeId == targetEmployeeId && x.Year == targetYear)
            .ToDictionaryAsync(x => x.LeaveTypeId);
        return Ok(types.Select(type =>
        {
            balances.TryGetValue(type.Id, out var balance);
            var opening = balance?.OpeningBalance ?? 0;
            var accrued = balance?.Accrued ?? type.AnnualEntitlement;
            var used = balance?.Used ?? 0;
            var adjustment = balance?.Adjustment ?? 0;
            return new { type.Id, type.Code, type.Name, Year = targetYear, OpeningBalance = opening, Accrued = accrued, Used = used, Adjustment = adjustment, Available = opening + accrued + adjustment - used };
        }));
    }

    [Authorize(Roles = "HR,Admin")]
    [HttpPost("balances/adjust")]
    public async Task<IActionResult> AdjustBalance(AdjustLeaveBalanceDto dto)
    {
        if (!await _db.Employees.AnyAsync(x => x.Id == dto.EmployeeId) || !await _db.LeaveTypes.AnyAsync(x => x.Id == dto.LeaveTypeId))
            return BadRequest(new { message = "Employee or leave type does not exist." });
        var balance = await _db.LeaveBalances.SingleOrDefaultAsync(x => x.EmployeeId == dto.EmployeeId && x.LeaveTypeId == dto.LeaveTypeId && x.Year == dto.Year);
        if (balance == null)
        {
            var entitlement = await _db.LeaveTypes.Where(x => x.Id == dto.LeaveTypeId).Select(x => x.AnnualEntitlement).SingleAsync();
            balance = new LeaveBalance { EmployeeId = dto.EmployeeId, LeaveTypeId = dto.LeaveTypeId, Year = dto.Year, Accrued = entitlement };
            _db.LeaveBalances.Add(balance);
        }
        balance.Adjustment += dto.Adjustment; balance.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests([FromQuery] LeaveStatus? status = null)
    {
        var account = await CurrentAccountAsync();
        if (account == null) return Unauthorized();
        var query = _db.LeaveRequests.AsNoTracking()
            .Include(x => x.Employee).ThenInclude(x => x.Department)
            .Include(x => x.LeaveType)
            .Include(x => x.ReviewedByUserAccount)
            .AsQueryable();

        if (!(User.IsInRole("HR") || User.IsInRole("Admin")))
        {
            if (!account.EmployeeId.HasValue) return Ok(Array.Empty<object>());
            var employeeId = account.EmployeeId.Value;
            query = User.IsInRole("Manager")
                ? query.Where(x => x.EmployeeId == employeeId || x.Employee.ManagerId == employeeId)
                : query.Where(x => x.EmployeeId == employeeId);
        }
        if (status.HasValue) query = query.Where(x => x.Status == status);
        var requests = await query.OrderByDescending(x => x.AppliedOn).ToListAsync();
        return Ok(requests.Select(MapRequest));
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest(CreateLeaveRequestDto dto)
    {
        var account = await CurrentAccountAsync();
        if (account == null) return Unauthorized();
        var canSubmitForOthers = User.IsInRole("HR") || User.IsInRole("Admin");
        var employeeId = canSubmitForOthers && dto.EmployeeId.HasValue ? dto.EmployeeId : account.EmployeeId;
        if (!employeeId.HasValue) return BadRequest(new { message = "Link this user account to an employee profile before requesting leave." });

        var employee = await _db.Employees.FindAsync(employeeId.Value);
        var leaveType = await _db.LeaveTypes.FindAsync(dto.LeaveTypeId);
        if (employee == null || leaveType is not { IsActive: true }) return BadRequest(new { message = "Employee or leave type is invalid." });
        if (!canSubmitForOthers && dto.StartDate.Date < DateTime.UtcNow.Date) return BadRequest(new { message = "Leave cannot start in the past." });
        if (dto.EndDate.Date < dto.StartDate.Date) return BadRequest(new { message = "End date cannot be before start date." });
        if (dto.StartDate.Year != dto.EndDate.Year) return BadRequest(new { message = "A leave request cannot cross calendar years." });
        if (dto.IsHalfDay && (dto.StartDate.Date != dto.EndDate.Date || !leaveType.AllowsHalfDay))
            return BadRequest(new { message = "Half-day leave is not available for this selection." });
        if (!string.IsNullOrWhiteSpace(leaveType.ApplicableGender) && !string.Equals(leaveType.ApplicableGender, employee.Gender, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "This leave type is not applicable to the employee." });

        var numberOfDays = dto.IsHalfDay ? 0.5m : CountWeekdays(dto.StartDate.Date, dto.EndDate.Date);
        if (numberOfDays <= 0) return BadRequest(new { message = "The selected period contains no working days." });
        if (leaveType.MaxConsecutiveDays.HasValue && numberOfDays > leaveType.MaxConsecutiveDays)
            return BadRequest(new { message = $"This leave type allows at most {leaveType.MaxConsecutiveDays} consecutive days." });
        var overlaps = await _db.LeaveRequests.AnyAsync(x => x.EmployeeId == employeeId &&
            (x.Status == LeaveStatus.Pending || x.Status == LeaveStatus.Approved) &&
            x.StartDate <= dto.EndDate.Date && x.EndDate >= dto.StartDate.Date);
        if (overlaps) return Conflict(new { message = "This request overlaps another pending or approved leave request." });

        var request = new LeaveRequest
        {
            EmployeeId = employeeId.Value, LeaveTypeId = dto.LeaveTypeId,
            StartDate = dto.StartDate.Date, EndDate = dto.EndDate.Date,
            NumberOfDays = numberOfDays, IsHalfDay = dto.IsHalfDay,
            Reason = dto.Reason.Trim(), ContactDuringLeave = Clean(dto.ContactDuringLeave)
        };
        _db.LeaveRequests.Add(request); await _db.SaveChangesAsync();
        return Created(string.Empty, new { request.Id, request.Status, request.NumberOfDays });
    }

    [Authorize(Roles = "Manager,HR,Admin")]
    [HttpPut("requests/{id:int}/review")]
    public async Task<IActionResult> ReviewRequest(int id, ReviewLeaveRequestDto dto)
    {
        if (dto.Status is not (LeaveStatus.Approved or LeaveStatus.Rejected))
            return BadRequest(new { message = "A review must approve or reject the request." });
        var account = await CurrentAccountAsync();
        if (account == null) return Unauthorized();
        var request = await _db.LeaveRequests.Include(x => x.Employee).Include(x => x.LeaveType).SingleOrDefaultAsync(x => x.Id == id);
        if (request == null) return NotFound();
        if (request.Status != LeaveStatus.Pending) return Conflict(new { message = "Only pending requests can be reviewed." });
        if (account.EmployeeId == request.EmployeeId) return BadRequest(new { message = "You cannot approve your own leave request." });
        if (User.IsInRole("Manager") && !(User.IsInRole("HR") || User.IsInRole("Admin")) && request.Employee.ManagerId != account.EmployeeId)
            return Forbid();

        if (dto.Status == LeaveStatus.Approved && request.LeaveType.IsPaid &&
            (request.LeaveType.AnnualEntitlement > 0 || request.LeaveType.Code == "COMP_OFF"))
        {
            var balance = await GetOrCreateBalanceAsync(request.EmployeeId, request.LeaveType, request.StartDate.Year);
            var available = balance.OpeningBalance + balance.Accrued + balance.Adjustment - balance.Used;
            if (available < request.NumberOfDays)
                return Conflict(new { message = $"Insufficient {request.LeaveType.Name} balance. Available: {available}." });
            balance.Used += request.NumberOfDays; balance.UpdatedAtUtc = DateTime.UtcNow;
        }

        request.Status = dto.Status; request.ReviewedByUserAccountId = account.Id;
        request.ReviewedAtUtc = DateTime.UtcNow; request.ReviewComment = Clean(dto.Comment);
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpPut("requests/{id:int}/cancel")]
    public async Task<IActionResult> CancelRequest(int id)
    {
        var account = await CurrentAccountAsync();
        if (account == null) return Unauthorized();
        var request = await _db.LeaveRequests.FindAsync(id);
        if (request == null) return NotFound();
        if (!(User.IsInRole("HR") || User.IsInRole("Admin")) && request.EmployeeId != account.EmployeeId) return Forbid();
        if (request.Status != LeaveStatus.Pending) return Conflict(new { message = "Only pending leave requests can be cancelled." });
        request.Status = LeaveStatus.Cancelled; request.CancelledAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(); return NoContent();
    }

    private async Task<UserAccount?> CurrentAccountAsync()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(value, out var id) ? await _db.UserAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id) : null;
    }

    private async Task<bool> CanAccessEmployeeAsync(UserAccount account, int employeeId)
    {
        if (User.IsInRole("HR") || User.IsInRole("Admin") || account.EmployeeId == employeeId) return true;
        return User.IsInRole("Manager") && account.EmployeeId.HasValue && await _db.Employees.AnyAsync(x => x.Id == employeeId && x.ManagerId == account.EmployeeId);
    }

    private async Task<LeaveBalance> GetOrCreateBalanceAsync(int employeeId, LeaveType leaveType, int year)
    {
        var balance = await _db.LeaveBalances.SingleOrDefaultAsync(x => x.EmployeeId == employeeId && x.LeaveTypeId == leaveType.Id && x.Year == year);
        if (balance != null) return balance;
        balance = new LeaveBalance { EmployeeId = employeeId, LeaveTypeId = leaveType.Id, Year = year, Accrued = leaveType.AnnualEntitlement };
        _db.LeaveBalances.Add(balance); return balance;
    }

    private static object MapRequest(LeaveRequest x) => new
    {
        x.Id, x.EmployeeId, EmployeeName = $"{x.Employee.FirstName} {x.Employee.LastName}",
        x.Employee.EmployeeCode, DepartmentName = x.Employee.Department?.Name,
        x.LeaveTypeId, LeaveTypeName = x.LeaveType.Name, x.StartDate, x.EndDate,
        x.NumberOfDays, x.IsHalfDay, x.Reason, x.ContactDuringLeave,
        Status = x.Status.ToString(), x.AppliedOn, x.ReviewedAtUtc, x.ReviewComment,
        ReviewedBy = x.ReviewedByUserAccount?.FullName
    };

    private static decimal CountWeekdays(DateTime start, DateTime end)
    {
        decimal days = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) days++;
        return days;
    }

    private static void ApplyTypeDto(LeaveType type, LeaveTypeWriteDto dto)
    {
        type.Code = dto.Code.Trim().ToUpperInvariant(); type.Name = dto.Name.Trim();
        type.Description = Clean(dto.Description); type.AnnualEntitlement = dto.AnnualEntitlement;
        type.CarryForwardLimit = dto.CarryForwardLimit; type.MaxConsecutiveDays = dto.MaxConsecutiveDays;
        type.DocumentRequiredAfterDays = dto.DocumentRequiredAfterDays; type.IsPaid = dto.IsPaid;
        type.AllowsHalfDay = dto.AllowsHalfDay; type.IsActive = dto.IsActive;
        type.ApplicableGender = Clean(dto.ApplicableGender); type.SortOrder = dto.SortOrder;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
