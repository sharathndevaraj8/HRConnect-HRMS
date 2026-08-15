namespace HRConnect.Application.DTOs;

public class EmployeeSummaryDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
    public DateTime DateOfJoining { get; set; }
}

public sealed class EmployeeDto : EmployeeSummaryDto
{
    public string EmploymentType { get; set; } = string.Empty;
    public string? WorkLocation { get; set; }
    public DateTime? DateOfLeaving { get; set; }
}

public sealed class PersonalEmployeeDto
{
    public string? PersonalEmail { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? AlternatePhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? BloodGroup { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
}
