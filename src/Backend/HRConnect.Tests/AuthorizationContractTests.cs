using HRConnect.API.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace HRConnect.Tests;

public sealed class AuthorizationContractTests
{
    [Fact] public void Employee_directory_requires_authentication() => Assert.NotNull(typeof(EmployeesController).GetCustomAttributes(typeof(AuthorizeAttribute), true).SingleOrDefault());
    [Fact] public void Employee_directory_requires_hr_or_admin() => Assert.Equal("HR,Admin", typeof(EmployeesController).GetMethod("GetAll")!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Roles);
    [Fact] public void Admin_delete_requires_admin_role() => Assert.Equal("Admin", typeof(EmployeesController).GetMethod("Delete")!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Roles);
}
