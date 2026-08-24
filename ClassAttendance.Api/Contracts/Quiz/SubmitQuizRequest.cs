using System.ComponentModel.DataAnnotations;

namespace ClassAttendance.Api.Contracts.Quiz;

public sealed class SubmitQuizRequest
{
    [Required]
    public IReadOnlyList<QuizAnswerRequest> Answers { get; init; } = [];
}

public sealed class QuizAnswerRequest
{
    [Range(1, int.MaxValue)]
    public int QuestionId { get; init; }

    [StringLength(4000)]
    public string? AnswerText { get; init; }

    public int? SelectedOptionId { get; init; }
}
