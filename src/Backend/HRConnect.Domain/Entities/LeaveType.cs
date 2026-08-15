namespace HRConnect.Domain.Entities;

public sealed class LeaveType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal AnnualEntitlement { get; set; }
    public decimal CarryForwardLimit { get; set; }
    public decimal? MaxConsecutiveDays { get; set; }
    public decimal? DocumentRequiredAfterDays { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool AllowsHalfDay { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? ApplicableGender { get; set; }
    public int SortOrder { get; set; }
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = [];
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = [];
}
