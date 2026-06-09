using System;
using System.Collections.Generic;
using System.Text;

namespace HRConnect.Domain.Entities
{
    public class Employee
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Designation { get; set; } = string.Empty;

        public DateTime DateOfJoining { get; set; }
    }
}
