using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Domain.Entities
{
    public class QuizOption
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string Text { get; set; } = null!;
        public bool IsCorrect { get; set; }     

        public QuizQuestion Question { get; set; } = null!;
    }
}
