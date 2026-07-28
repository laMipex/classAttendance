using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Domain.Entities
{
    public class QuizSession
    {
        public int Id { get; set; }
        public int LectureId { get; set; }
        public string Title { get; set; } = null!;

        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }

        public bool isActive { get; set; }    

        public Lecture Lecture { get; set; } = null!;
    }
}
