using HRConnect.Application.DTOs;
using HRConnect.Application.Interfaces;
using HRConnect.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRConnect.API.Controllers;

/// <summary>
/// API controller that exposes CRUD and list endpoints for employees.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private const int MaxPageSize = 100;
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    /// <summary>
    /// Retrieves a paginated list of employees. Optionally filters by a search string.
    /// </summary>
    /// <param name="search">Optional search filter (e.g. name, email, designation).</param>
    /// <param name="pageNumber">Page number (1-based). Values less than 1 are clamped to 1.</param>
    /// <param name="pageSize">Page size. Clamped between 1 and 100.</param>
    /// <returns>An ActionResult containing a paged response with Items, TotalCount, PageNumber, PageSize and TotalPages.</returns>
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var result = await _employeeService.GetEmployeesAsync(search, pageNumber, pageSize);
        var employeeDtos = result.Items.Select(MapToDto).ToList();

        return Ok(new
        {
            Items = employeeDtos,
            result.TotalCount,
            result.PageNumber,
            result.PageSize,
            result.TotalPages
        });
    }

    /// <summary>
    /// Gets an employee by ID.
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <returns>Employee information</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(id);

        if (employee == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(employee));
    }

    /// <summary>
    /// Creates a new employee.
    /// </summary>
    /// <param name="dto">Data for the employee to create.</param>
    /// <returns>Created employee DTO with assigned Id.</returns>
    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeDto dto)
    {
        var employee = new Employee
        {
            FirstName = NormalizeName(dto.FirstName),
            LastName = NormalizeName(dto.LastName),
            Email = NormalizeText(dto.Email).ToLowerInvariant(),
            Designation = NormalizeText(dto.Designation),
            DateOfJoining = dto.DateOfJoining
        };

        if (await _employeeService.EmailExistsAsync(employee.Email))
        {
            return Conflict(new { message = "An employee with this email already exists." });
        }

        await _employeeService.AddEmployeeAsync(employee);

        return CreatedAtAction(
            nameof(GetById),
            new { id = employee.Id },
            MapToDto(employee));
    }

    /// <summary>
    /// Updates an existing employee.
    /// </summary>
    /// <param name="id">Employee ID to update.</param>
    /// <param name="dto">Updated employee values.</param>
    /// <returns>NoContent on success; BadRequest when ids mismatch; NotFound when employee does not exist; Conflict when email already exists.</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, UpdateEmployeeDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest("Employee ID mismatch.");
        }

        var existingEmployee = await _employeeService.GetEmployeeByIdAsync(id);

        if (existingEmployee == null)
        {
            return NotFound();
        }

        var normalizedEmail = NormalizeText(dto.Email).ToLowerInvariant();

        if (await _employeeService.EmailExistsAsync(normalizedEmail, id))
        {
            return Conflict(new { message = "An employee with this email already exists." });
        }

        var employee = new Employee
        {
            Id = dto.Id,
            FirstName = NormalizeName(dto.FirstName),
            LastName = NormalizeName(dto.LastName),
            Email = normalizedEmail,
            Designation = NormalizeText(dto.Designation),
            DateOfJoining = dto.DateOfJoining
        };

        await _employeeService.UpdateEmployeeAsync(employee);

        return NoContent();
    }

    /// <summary>
    /// Deletes an employee by ID.
    /// </summary>
    /// <param name="id">Employee ID to delete.</param>
    /// <returns>NoContent on success; NotFound when the employee does not exist.</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(id);

        if (employee == null)
        {
            return NotFound();
        }

        await _employeeService.DeleteEmployeeAsync(id);

        return NoContent();
    }

    private static EmployeeDto MapToDto(Employee employee)
    {
        return new EmployeeDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Designation = employee.Designation,
            DateOfJoining = employee.DateOfJoining
        };
    }

    private static string NormalizeName(string value)
    {
        return string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeText(string value)
    {
        return value.Trim();
    }
}
