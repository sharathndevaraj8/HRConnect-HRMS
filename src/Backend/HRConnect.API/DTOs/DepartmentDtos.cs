using System.ComponentModel.DataAnnotations;

namespace HRConnect.Application.DTOs;

public class DepartmentWriteDto
{
    [Required, MaxLength(20)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class DepartmentDto : DepartmentWriteDto
{
    public int Id { get; set; }
    public int EmployeeCount { get; set; }
}
