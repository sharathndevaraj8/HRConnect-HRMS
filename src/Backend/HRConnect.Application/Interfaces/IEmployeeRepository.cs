using HRConnect.Application.Models;
using HRConnect.Domain.Entities;

namespace HRConnect.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<PagedResult<Employee>> GetAllAsync(string? search, int pageNumber, int pageSize);

    Task<Employee?> GetByIdAsync(int id);

    Task<bool> EmailExistsAsync(string email, int? excludingEmployeeId = null);

    Task AddAsync(Employee employee);

    Task UpdateAsync(Employee employee);

    Task DeleteAsync(int id);
}
