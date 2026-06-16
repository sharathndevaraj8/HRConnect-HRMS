using HRConnect.Application.Interfaces;
using HRConnect.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using HRConnect.Application.DTOs;

namespace HRConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll()
    {
        var employees = await _employeeService.GetAllEmployeesAsync();

        var employeeDtos = employees.Select(employee => new EmployeeDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Designation = employee.Designation,
            DateOfJoining = employee.DateOfJoining
        });
 
        return Ok(employeeDtos);
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

        var dto = new EmployeeDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Designation = employee.Designation,
            DateOfJoining = employee.DateOfJoining
        };

        return Ok(dto);
    }


    [HttpPost]
    public async Task<ActionResult> Create(CreateEmployeeDto dto)
    {
        var employee = new Employee
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Designation = dto.Designation,
            DateOfJoining = dto.DateOfJoining
        };

        await _employeeService.AddEmployeeAsync(employee);

        return CreatedAtAction(
            nameof(GetById),
            new { id = employee.Id },
            employee);
    }

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

        var employee = new Employee
        {
            Id = dto.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Designation = dto.Designation,
            DateOfJoining = dto.DateOfJoining
        };

        await _employeeService.UpdateEmployeeAsync(employee);

        return NoContent();
    }

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
}
