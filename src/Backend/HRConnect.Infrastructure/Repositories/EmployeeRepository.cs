using HRConnect.Application.Interfaces;
using HRConnect.Application.Models;
using HRConnect.Domain.Entities;
using HRConnect.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRConnect.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Employee employee)
    {
        await _context.Employees.AddAsync(employee);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var employee = await _context.Employees.FindAsync(id);

        if (employee != null)
        {
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<PagedResult<Employee>> GetAllAsync(string? search, int pageNumber, int pageSize)
    {
        IQueryable<Employee> query = _context.Employees
            .AsNoTracking()
            .Include(employee => employee.Department)
            .Include(employee => employee.Manager);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmedSearch = search.Trim();

            query = query.Where(employee =>
                employee.FirstName.Contains(trimmedSearch) ||
                employee.LastName.Contains(trimmedSearch) ||
                employee.Email.Contains(trimmedSearch) ||
                employee.EmployeeCode.Contains(trimmedSearch) ||
                employee.Designation.Contains(trimmedSearch) ||
                (employee.Department != null && employee.Department.Name.Contains(trimmedSearch)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(employee => employee.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Employee>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        return await _context.Employees
            .AsNoTracking()
            .Include(employee => employee.Department)
            .Include(employee => employee.Manager)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Employee?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Employees.AsNoTracking()
            .FirstOrDefaultAsync(employee => employee.Email.ToLower() == normalizedEmail);
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludingEmployeeId = null)
    {
        var normalizedEmail = email.Trim().ToLower();

        return await _context.Employees.AnyAsync(employee =>
            employee.Email.ToLower() == normalizedEmail &&
            (!excludingEmployeeId.HasValue || employee.Id != excludingEmployeeId.Value));
    }

    public async Task UpdateAsync(Employee employee)
    {
        _context.Employees.Update(employee);
        await _context.SaveChangesAsync();
    }
}
