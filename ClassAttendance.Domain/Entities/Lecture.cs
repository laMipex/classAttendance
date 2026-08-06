using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Domain.Entities
{
    public class Lecture
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public int ProfessorId { get; set; }

        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }

        public string? Rooms { get; set; }

        public Subject Subject { get; set; } = null!;
        public Professor Professor { get; set; } = null!;
        public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
    }
}
