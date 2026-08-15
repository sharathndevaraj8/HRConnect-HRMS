namespace HRConnect.Domain.Entities;

public sealed class LeaveBalance
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal Accrued { get; set; }
    public decimal Used { get; set; }
    public decimal Adjustment { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Employee Employee { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
}
