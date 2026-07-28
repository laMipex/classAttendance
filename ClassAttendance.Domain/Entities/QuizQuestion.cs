using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Domain.Entities
{
    public class QuizQuestion
    {
        public int Id { get; set; }
        public int QuizSessionId { get; set; }
        public string Text { get; set; } = null!;
        public string Type { get; set; } = null!;
        public int OrderNo { get; set; }

        public QuizSession QuizSession { get; set; } = null!;

    }
}
