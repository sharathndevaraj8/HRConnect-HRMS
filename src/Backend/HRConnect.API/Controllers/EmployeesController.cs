using System.Security.Claims;
using HRConnect.Application.DTOs;
using HRConnect.Application.Interfaces;
using HRConnect.Domain.Entities;
using HRConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRConnect.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class EmployeesController : ControllerBase
{
    private const int MaxPageSize = 100;
    private readonly IEmployeeService _employeeService;
    private readonly AppDbContext _dbContext;

    public EmployeesController(IEmployeeService employeeService, AppDbContext dbContext)
    {
        _employeeService = employeeService;
        _dbContext = dbContext;
    }

    [Authorize(Roles = "HR,Admin")]
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var result = await _employeeService.GetEmployeesAsync(search, pageNumber, pageSize);

        return Ok(new
        {
            Items = result.Items.Select(MapToSummaryDto).ToList(),
            result.TotalCount,
            result.PageNumber,
            result.PageSize,
            result.TotalPages
        });
    }

    [Authorize(Roles = "HR,Admin")]
    [HttpGet("options")]
    public async Task<ActionResult<IReadOnlyList<EmployeeSummaryDto>>> GetOptions()
    {
        var employees = await _dbContext.Employees
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Manager)
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToListAsync();

        return Ok(employees.Select(MapToSummaryDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        if (!await CanViewWorkDataAsync(id)) return Forbid();
        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        return employee == null ? NotFound() : Ok(MapToDto(employee));
    }

    [HttpGet("{id:int}/personal")]
    public async Task<ActionResult<PersonalEmployeeDto>> GetPersonal(int id)
    {
        if (!await IsLinkedEmployeeAsync(id)) return Forbid();
        var employee = await _dbContext.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        return employee == null ? NotFound() : Ok(MapToPersonalDto(employee));
    }

    [HttpPut("{id:int}/personal")]
    public async Task<IActionResult> UpdatePersonal(int id, UpdatePersonalEmployeeDto dto)
    {
        if (!await IsLinkedEmployeeAsync(id)) return Forbid();
        if (dto.DateOfBirth.HasValue && dto.DateOfBirth.Value.Date >= DateTime.UtcNow.Date)
            return BadRequest(new { message = "Date of birth must be in the past." });

        var employee = await _dbContext.Employees.FindAsync(id);
        if (employee == null) return NotFound();
        ApplyPersonalDto(employee, dto);
        employee.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "HR,Admin")]
    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeDto dto)
    {
        var validationError = await ValidateReferencesAsync(dto);
        if (validationError != null) return BadRequest(new { message = validationError });

        var employee = new Employee();
        ApplyWorkDto(employee, dto);
        employee.PhoneNumber = "Not provided";
        employee.Country = "India";
        employee.CreatedAtUtc = DateTime.UtcNow;

        if (await _employeeService.EmailExistsAsync(employee.Email))
            return Conflict(new { message = "An employee with this email already exists." });
        if (await _dbContext.Employees.AnyAsync(x => x.EmployeeCode == employee.EmployeeCode))
            return Conflict(new { message = "An employee with this employee code already exists." });

        await _employeeService.AddEmployeeAsync(employee);
        var created = await _employeeService.GetEmployeeByIdAsync(employee.Id) ?? employee;
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, MapToDto(created));
    }

    [Authorize(Roles = "HR,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEmployeeDto dto)
    {
        if (id != dto.Id) return BadRequest(new { message = "Employee ID mismatch." });
        var existing = await _employeeService.GetEmployeeByIdAsync(id);
        if (existing == null) return NotFound();

        var validationError = await ValidateReferencesAsync(dto, id);
        if (validationError != null) return BadRequest(new { message = validationError });

        var normalizedEmail = Normalize(dto.Email).ToLowerInvariant();
        var employeeCode = Normalize(dto.EmployeeCode).ToUpperInvariant();
        if (await _employeeService.EmailExistsAsync(normalizedEmail, id))
            return Conflict(new { message = "An employee with this email already exists." });
        if (await _dbContext.Employees.AnyAsync(x => x.Id != id && x.EmployeeCode == employeeCode))
            return Conflict(new { message = "An employee with this employee code already exists." });

        ApplyWorkDto(existing, dto);
        existing.Department = null;
        existing.Manager = null;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await _employeeService.UpdateEmployeeAsync(existing);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _dbContext.Employees.AnyAsync(x => x.Id == id)) return NotFound();
        await _employeeService.DeleteEmployeeAsync(id);
        return NoContent();
    }

    private async Task<string?> ValidateReferencesAsync(CreateEmployeeDto dto, int? employeeId = null)
    {
        if (dto.DepartmentId.HasValue && !await _dbContext.Departments.AnyAsync(x => x.Id == dto.DepartmentId && x.IsActive))
            return "The selected department does not exist or is inactive.";
        if (dto.ManagerId.HasValue)
        {
            if (dto.ManagerId == employeeId) return "An employee cannot be their own manager.";
            if (!await _dbContext.Employees.AnyAsync(x => x.Id == dto.ManagerId)) return "The selected manager does not exist.";
        }
        if (dto.DateOfLeaving.HasValue && dto.DateOfLeaving.Value.Date < dto.DateOfJoining.Date)
            return "Date of leaving cannot be before date of joining.";
        return null;
    }

    private async Task<bool> CanViewWorkDataAsync(int employeeId)
    {
        if (User.IsInRole("HR") || User.IsInRole("Admin")) return true;
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return false;
        var linkedEmployeeId = await _dbContext.UserAccounts
            .Where(x => x.Id == userId)
            .Select(x => x.EmployeeId)
            .SingleOrDefaultAsync();
        if (linkedEmployeeId == employeeId) return true;
        return User.IsInRole("Manager") && linkedEmployeeId.HasValue &&
            await _dbContext.Employees.AnyAsync(x => x.Id == employeeId && x.ManagerId == linkedEmployeeId);
    }

    private async Task<bool> IsLinkedEmployeeAsync(int employeeId)
    {
        if (User.IsInRole("Admin")) return true;

        var claimedEmployeeId = GetClaimedEmployeeId();
        if (claimedEmployeeId.HasValue) return claimedEmployeeId == employeeId;

        var userId = GetCurrentUserId();
        return userId.HasValue && await _dbContext.UserAccounts
            .AnyAsync(x => x.Id == userId && x.EmployeeId == employeeId);
    }

    private int? GetClaimedEmployeeId()
    {
        var value = User.FindFirstValue("employee_id");
        return int.TryParse(value, out var id) ? id : null;
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(value, out var id) ? id : null;
    }

    private static void ApplyWorkDto(Employee employee, CreateEmployeeDto dto)
    {
        employee.EmployeeCode = Normalize(dto.EmployeeCode).ToUpperInvariant();
        employee.FirstName = Normalize(dto.FirstName);
        employee.LastName = Normalize(dto.LastName);
        employee.Email = Normalize(dto.Email).ToLowerInvariant();
        employee.Designation = Normalize(dto.Designation);
        employee.DepartmentId = dto.DepartmentId;
        employee.ManagerId = dto.ManagerId;
        employee.EmploymentType = Normalize(dto.EmploymentType);
        employee.EmploymentStatus = Normalize(dto.EmploymentStatus);
        employee.WorkLocation = NormalizeNullable(dto.WorkLocation);
        employee.DateOfJoining = dto.DateOfJoining.Date;
        employee.DateOfLeaving = dto.DateOfLeaving?.Date;
    }

    private static void ApplyPersonalDto(Employee employee, UpdatePersonalEmployeeDto dto)
    {
        employee.PersonalEmail = NormalizeNullable(dto.PersonalEmail)?.ToLowerInvariant();
        employee.PhoneNumber = Normalize(dto.PhoneNumber);
        employee.AlternatePhoneNumber = NormalizeNullable(dto.AlternatePhoneNumber);
        employee.DateOfBirth = dto.DateOfBirth?.Date;
        employee.Gender = NormalizeNullable(dto.Gender);
        employee.MaritalStatus = NormalizeNullable(dto.MaritalStatus);
        employee.BloodGroup = NormalizeNullable(dto.BloodGroup)?.ToUpperInvariant();
        employee.AddressLine1 = NormalizeNullable(dto.AddressLine1);
        employee.AddressLine2 = NormalizeNullable(dto.AddressLine2);
        employee.City = NormalizeNullable(dto.City);
        employee.State = NormalizeNullable(dto.State);
        employee.PostalCode = NormalizeNullable(dto.PostalCode);
        employee.Country = NormalizeNullable(dto.Country);
        employee.EmergencyContactName = NormalizeNullable(dto.EmergencyContactName);
        employee.EmergencyContactRelationship = NormalizeNullable(dto.EmergencyContactRelationship);
        employee.EmergencyContactPhone = NormalizeNullable(dto.EmergencyContactPhone);
    }

    private static EmployeeSummaryDto MapToSummaryDto(Employee x) => new()
    {
        Id = x.Id, EmployeeCode = x.EmployeeCode, FirstName = x.FirstName, LastName = x.LastName,
        Email = x.Email, Designation = x.Designation, DepartmentId = x.DepartmentId,
        DepartmentName = x.Department?.Name, ManagerId = x.ManagerId,
        ManagerName = x.Manager == null ? null : $"{x.Manager.FirstName} {x.Manager.LastName}",
        EmploymentStatus = x.EmploymentStatus, DateOfJoining = x.DateOfJoining
    };

    private static EmployeeDto MapToDto(Employee x)
    {
        var dto = new EmployeeDto
        {
            EmploymentType = x.EmploymentType, WorkLocation = x.WorkLocation,
            DateOfLeaving = x.DateOfLeaving
        };
        var summary = MapToSummaryDto(x);
        dto.Id = summary.Id; dto.EmployeeCode = summary.EmployeeCode; dto.FirstName = summary.FirstName;
        dto.LastName = summary.LastName; dto.Email = summary.Email; dto.Designation = summary.Designation;
        dto.DepartmentId = summary.DepartmentId; dto.DepartmentName = summary.DepartmentName;
        dto.ManagerId = summary.ManagerId; dto.ManagerName = summary.ManagerName;
        dto.EmploymentStatus = summary.EmploymentStatus; dto.DateOfJoining = summary.DateOfJoining;
        return dto;
    }

    private static PersonalEmployeeDto MapToPersonalDto(Employee x) => new()
    {
        PersonalEmail = x.PersonalEmail, PhoneNumber = x.PhoneNumber,
        AlternatePhoneNumber = x.AlternatePhoneNumber, DateOfBirth = x.DateOfBirth,
        Gender = x.Gender, MaritalStatus = x.MaritalStatus, BloodGroup = x.BloodGroup,
        AddressLine1 = x.AddressLine1, AddressLine2 = x.AddressLine2,
        City = x.City, State = x.State, PostalCode = x.PostalCode, Country = x.Country,
        EmergencyContactName = x.EmergencyContactName,
        EmergencyContactRelationship = x.EmergencyContactRelationship,
        EmergencyContactPhone = x.EmergencyContactPhone
    };

    private static string Normalize(string value) => string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : Normalize(value);
}
