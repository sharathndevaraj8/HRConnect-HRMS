using HRConnect.Domain.Enums;

namespace HRConnect.Domain.Entities;

public sealed class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal NumberOfDays { get; set; }
    public bool IsHalfDay { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ContactDuringLeave { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public DateTime AppliedOn { get; set; } = DateTime.UtcNow;
    public int? ReviewedByUserAccountId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public Employee Employee { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
    public UserAccount? ReviewedByUserAccount { get; set; }
}
