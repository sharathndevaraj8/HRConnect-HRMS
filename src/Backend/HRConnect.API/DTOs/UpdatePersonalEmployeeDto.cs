using System.ComponentModel.DataAnnotations;

namespace HRConnect.Application.DTOs;

public sealed class UpdatePersonalEmployeeDto
{
    [EmailAddress, MaxLength(255)] public string? PersonalEmail { get; set; }
    [Required, Phone, MaxLength(20)] public string PhoneNumber { get; set; } = string.Empty;
    [Phone, MaxLength(20)] public string? AlternatePhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    [MaxLength(30)] public string? Gender { get; set; }
    [MaxLength(30)] public string? MaritalStatus { get; set; }
    [MaxLength(10)] public string? BloodGroup { get; set; }
    [MaxLength(255)] public string? AddressLine1 { get; set; }
    [MaxLength(255)] public string? AddressLine2 { get; set; }
    [MaxLength(100)] public string? City { get; set; }
    [MaxLength(100)] public string? State { get; set; }
    [MaxLength(12)] public string? PostalCode { get; set; }
    [MaxLength(100)] public string? Country { get; set; }
    [MaxLength(100)] public string? EmergencyContactName { get; set; }
    [MaxLength(50)] public string? EmergencyContactRelationship { get; set; }
    [Phone, MaxLength(20)] public string? EmergencyContactPhone { get; set; }
}
