using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Domain.Entities
{
    public class StudyProgram
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Year { get; set; }   

        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
