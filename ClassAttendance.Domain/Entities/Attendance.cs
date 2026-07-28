using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Domain.Entities
{
    public class Attendance
    {
        public int Id { get; set; }
        public int AttendanceSessionId { get; set; }
        public int StudentId { get; set; }

        public DateTime CheckInAt { get; set; }

        public string Method { get; set; } = null!;
        public string Status { get; set; } = null!;

        public AttendanceSession AttendanceSession { get; set; } = null!;
        public Student Student { get; set; } = null!;
    }
}
