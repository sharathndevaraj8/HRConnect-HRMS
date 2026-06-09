using System;
using System.Collections.Generic;
using System.Text;
using HRConnect.Domain.Enums;

namespace HRConnect.Domain.Entities
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Reason { get; set; } = string.Empty;

        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

        public DateTime AppliedOn { get; set; } = DateTime.UtcNow;
    }
}
