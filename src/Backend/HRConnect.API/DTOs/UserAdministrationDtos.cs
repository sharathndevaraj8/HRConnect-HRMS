using System.ComponentModel.DataAnnotations;

namespace HRConnect.Application.DTOs;

public sealed class ChangeUserRoleDto
{
    [Required] public string Role { get; set; } = string.Empty;
}

public sealed class LinkUserEmployeeDto
{
    public int? EmployeeId { get; set; }
}
