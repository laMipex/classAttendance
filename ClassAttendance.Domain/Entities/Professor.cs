using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Domain.Entities
{
    public class Professor
    {
        public int UserId { get; set; }
        public string EmployeeCode { get; set; } = null!;

        public User User { get; set; } = null!;
    }
}
