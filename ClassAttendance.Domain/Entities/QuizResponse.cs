using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Domain.Entities
{
    public class QuizResponse
    {
        public int Id { get; set; }
        public int QuizSessionId { get; set; }
        public int QuestionId { get; set; }
        public int StudentId { get; set; }

        public string? AnswerText { get; set; }
        public int? SelectedOptionId { get; set; }

        public DateTime SubmittedAt { get; set; }

        public QuizSession QuizSession { get; set; } = null!;   
        public QuizQuestion Question { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public QuizOption? SelectedOption { get; set; }
    }
}
