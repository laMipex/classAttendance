using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Domain.Entities
{
    public class Enrollment
    {
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public string AcademicYear { get; set; } = null!;
        public string Status { get; set; } = null!;

        public Student Student { get; set; } = null!;
        public Subject Subject { get; set; } = null!;
    }
}
