using HRConnect.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRConnect.Domain.Interfaces;
public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int id);

    Task<IEnumerable<Employee>> GetAllAsync();

    Task AddAsync(Employee employee);

    Task UpdateAsync(Employee employee);

    Task DeleteAsync(int id);
}
