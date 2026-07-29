using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Domain.Entities
{
    public class SubjectProfessor
    {
        public int SubjectId { get; set; }
        public int ProfessorId { get; set; }

        public Subject Subject { get; set; } = null!;
        public Professor Professor { get; set; } = null!;
    }
}
