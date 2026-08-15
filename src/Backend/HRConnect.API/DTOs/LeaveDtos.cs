using System.ComponentModel.DataAnnotations;
using HRConnect.Domain.Enums;

namespace HRConnect.Application.DTOs;

public sealed class LeaveTypeWriteDto
{
    [Required, MaxLength(30)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    [Range(0, 366)] public decimal AnnualEntitlement { get; set; }
    [Range(0, 366)] public decimal CarryForwardLimit { get; set; }
    [Range(0.5, 366)] public decimal? MaxConsecutiveDays { get; set; }
    [Range(0.5, 366)] public decimal? DocumentRequiredAfterDays { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool AllowsHalfDay { get; set; } = true;
    public bool IsActive { get; set; } = true;
    [MaxLength(30)] public string? ApplicableGender { get; set; }
    public int SortOrder { get; set; }
}

public sealed class CreateLeaveRequestDto
{
    public int? EmployeeId { get; set; }
    [Required] public int LeaveTypeId { get; set; }
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
    public bool IsHalfDay { get; set; }
    [Required, MinLength(3), MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    [MaxLength(100)] public string? ContactDuringLeave { get; set; }
}

public sealed class ReviewLeaveRequestDto
{
    [Required] public LeaveStatus Status { get; set; }
    [MaxLength(1000)] public string? Comment { get; set; }
}

public sealed class AdjustLeaveBalanceDto
{
    [Required] public int EmployeeId { get; set; }
    [Required] public int LeaveTypeId { get; set; }
    [Range(2000, 2200)] public int Year { get; set; } = DateTime.UtcNow.Year;
    [Range(-366, 366)] public decimal Adjustment { get; set; }
}
