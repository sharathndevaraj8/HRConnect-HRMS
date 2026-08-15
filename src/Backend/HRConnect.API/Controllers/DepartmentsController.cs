using HRConnect.Application.DTOs;
using HRConnect.Domain.Entities;
using HRConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRConnect.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public DepartmentsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.Departments.AsNoTracking();
        if (!includeInactive) query = query.Where(x => x.IsActive);
        return Ok(await query.OrderBy(x => x.Name).Select(x => new DepartmentDto
        {
            Id = x.Id, Code = x.Code, Name = x.Name, Description = x.Description,
            IsActive = x.IsActive, EmployeeCount = x.Employees.Count
        }).ToListAsync());
    }

    [Authorize(Roles = "HR,Admin")]
    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create(DepartmentWriteDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        var name = dto.Name.Trim();
        if (await _db.Departments.AnyAsync(x => x.Code == code || x.Name == name))
            return Conflict(new { message = "A department with this code or name already exists." });
        var department = new Department { Code = code, Name = name, Description = Clean(dto.Description), IsActive = dto.IsActive };
        _db.Departments.Add(department);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new DepartmentDto { Id = department.Id, Code = code, Name = name, Description = department.Description, IsActive = department.IsActive });
    }

    [Authorize(Roles = "HR,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, DepartmentWriteDto dto)
    {
        var department = await _db.Departments.FindAsync(id);
        if (department == null) return NotFound();
        var code = dto.Code.Trim().ToUpperInvariant();
        var name = dto.Name.Trim();
        if (await _db.Departments.AnyAsync(x => x.Id != id && (x.Code == code || x.Name == name)))
            return Conflict(new { message = "A department with this code or name already exists." });
        department.Code = code; department.Name = name; department.Description = Clean(dto.Description); department.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _db.Departments.Include(x => x.Employees).SingleOrDefaultAsync(x => x.Id == id);
        if (department == null) return NotFound();
        if (department.Employees.Count != 0) return Conflict(new { message = "Move employees out of this department before deleting it." });
        _db.Departments.Remove(department); await _db.SaveChangesAsync(); return NoContent();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
