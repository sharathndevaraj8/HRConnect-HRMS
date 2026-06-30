using HRConnect.Application.Models;
using HRConnect.Domain.Entities;

namespace HRConnect.Application.Interfaces;

public interface IEmployeeService
{
    Task<PagedResult<Employee>> GetEmployeesAsync(string? search, int pageNumber, int pageSize);

    Task<Employee?> GetEmployeeByIdAsync(int id);

    Task<bool> EmailExistsAsync(string email, int? excludingEmployeeId = null);

    Task AddEmployeeAsync(Employee employee);

    Task UpdateEmployeeAsync(Employee employee);

    Task DeleteEmployeeAsync(int id);
}
