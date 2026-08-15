using System.ComponentModel.DataAnnotations;

namespace HRConnect.Application.DTOs;

public class CreateEmployeeDto
{
    [Required, MaxLength(30)] public string EmployeeCode { get; set; } = string.Empty;
    [Required, MinLength(2), MaxLength(50)] public string FirstName { get; set; } = string.Empty;
    [Required, MinLength(2), MaxLength(50)] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(255)] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Designation { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public int? ManagerId { get; set; }
    [Required, MaxLength(30)] public string EmploymentType { get; set; } = "Permanent";
    [Required, MaxLength(30)] public string EmploymentStatus { get; set; } = "Active";
    [MaxLength(100)] public string? WorkLocation { get; set; }
    [Required] public DateTime DateOfJoining { get; set; }
    public DateTime? DateOfLeaving { get; set; }
}
