using HRConnect.Application.DTOs;
using HRConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRConnect.API.Controllers;

[ApiController]
[Authorize(Roles = "HR,Admin")]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private static readonly HashSet<string> Roles = new(StringComparer.OrdinalIgnoreCase) { "Employee", "Manager", "HR", "Admin" };
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _db.UserAccounts.AsNoTracking().Include(x => x.Employee)
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.Id, x.FullName, x.Email, x.Role, x.IsActive, x.EmployeeId,
                EmployeeName = x.Employee == null ? null : x.Employee.FirstName + " " + x.Employee.LastName,
                x.CreatedAtUtc
            }).ToListAsync());
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> ChangeRole(int id, ChangeUserRoleDto dto)
    {
        if (!Roles.Contains(dto.Role)) return BadRequest(new { message = "Role must be Employee, Manager, HR, or Admin." });
        var user = await _db.UserAccounts.FindAsync(id); if (user == null) return NotFound();
        user.Role = Roles.Single(x => string.Equals(x, dto.Role, StringComparison.OrdinalIgnoreCase));
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpPut("{id:int}/employee")]
    public async Task<IActionResult> LinkEmployee(int id, LinkUserEmployeeDto dto)
    {
        var user = await _db.UserAccounts.FindAsync(id); if (user == null) return NotFound();
        if (dto.EmployeeId.HasValue)
        {
            if (!await _db.Employees.AnyAsync(x => x.Id == dto.EmployeeId)) return BadRequest(new { message = "Employee does not exist." });
            if (await _db.UserAccounts.AnyAsync(x => x.Id != id && x.EmployeeId == dto.EmployeeId))
                return Conflict(new { message = "That employee is already linked to another user." });
        }
        user.EmployeeId = dto.EmployeeId; await _db.SaveChangesAsync(); return NoContent();
    }
}
