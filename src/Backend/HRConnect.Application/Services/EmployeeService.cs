using HRConnect.Application.Interfaces;
using HRConnect.Application.Models;
using HRConnect.Domain.Entities;

namespace HRConnect.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<PagedResult<Employee>> GetEmployeesAsync(string? search, int pageNumber, int pageSize)
    {
        return await _employeeRepository.GetAllAsync(search, pageNumber, pageSize);
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int id)
    {
        return await _employeeRepository.GetByIdAsync(id);
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludingEmployeeId = null)
    {
        return await _employeeRepository.EmailExistsAsync(email, excludingEmployeeId);
    }

    public async Task AddEmployeeAsync(Employee employee)
    {
        await _employeeRepository.AddAsync(employee);
    }

    public async Task UpdateEmployeeAsync(Employee employee)
    {
        await _employeeRepository.UpdateAsync(employee);
    }

    public async Task DeleteEmployeeAsync(int id)
    {
        await _employeeRepository.DeleteAsync(id);
    }
}
