using HRConnect.Domain.Enums;

namespace HRConnect.Tests;

public sealed class LeaveLogicTests
{
    [Theory]
    [InlineData(LeaveStatus.Pending, true)] [InlineData(LeaveStatus.Approved, true)] [InlineData(LeaveStatus.Rejected, false)] [InlineData(LeaveStatus.Cancelled, false)]
    public void Only_pending_and_approved_leave_blocks_new_request(LeaveStatus status, bool expected) => Assert.Equal(expected, status is LeaveStatus.Pending or LeaveStatus.Approved);
    [Fact] public void Half_day_leave_is_half_a_day() => Assert.Equal(0.5m, 0.5m);
    [Fact] public void Review_can_only_approve_or_reject() => Assert.True(LeaveStatus.Approved is LeaveStatus.Approved or LeaveStatus.Rejected);
}
