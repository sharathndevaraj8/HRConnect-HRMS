using HRConnect.Application.Interfaces;
using HRConnect.Application.Models;
using HRConnect.Application.Services;
using HRConnect.Domain.Entities;

namespace HRConnect.Tests;

public sealed class EmployeeServiceTests
{
    [Fact] public async Task Gets_employee_by_id() => Assert.Equal(7, (await new EmployeeService(new RepositoryStub { Employee = new Employee { Id = 7 } }).GetEmployeeByIdAsync(7))!.Id);
    [Fact] public async Task Returns_null_when_employee_is_missing() => Assert.Null(await new EmployeeService(new RepositoryStub()).GetEmployeeByIdAsync(42));
    [Fact] public async Task Passes_email_exclusion_to_repository() { var repo = new RepositoryStub { EmailExists = true }; Assert.True(await new EmployeeService(repo).EmailExistsAsync("a@b.com", 4)); Assert.Equal(4, repo.ExcludedId); }
    [Fact] public async Task Adds_employee() { var repo = new RepositoryStub(); await new EmployeeService(repo).AddEmployeeAsync(new Employee()); Assert.True(repo.Added); }

    private sealed class RepositoryStub : IEmployeeRepository
    {
        public Employee? Employee { get; init; } public bool EmailExists { get; init; } public int? ExcludedId { get; private set; } public bool Added { get; private set; }
        public Task<PagedResult<Employee>> GetAllAsync(string? s,int p,int z) => Task.FromResult(new PagedResult<Employee>());
        public Task<Employee?> GetByIdAsync(int id) => Task.FromResult(Employee?.Id == id ? Employee : null);
        public Task<bool> EmailExistsAsync(string e,int? id=null) { ExcludedId=id; return Task.FromResult(EmailExists); }
        public Task AddAsync(Employee e) { Added=true; return Task.CompletedTask; } public Task UpdateAsync(Employee e)=>Task.CompletedTask; public Task DeleteAsync(int id)=>Task.CompletedTask;
    }
}
