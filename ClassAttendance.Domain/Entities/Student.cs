using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Domain.Entities
{
    public class Student
    {
        public int UserId { get; set; }
        public string Index { get; set; } = null!;
        public int StudyProgramId { get; set; }

        public User User { get; set; } = null!;
        public StudyProgram StudyProgram { get; set; } = null!;
    }
}
