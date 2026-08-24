using System.ComponentModel.DataAnnotations;

namespace ClassAttendance.Api.Contracts.Quiz;

public sealed class CreateQuizRequest
{
    [Range(1, int.MaxValue)]
    public int LectureId { get; init; }

    [Required]
    [StringLength(200)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Question { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^(Text|Choice)$")]
    public string QuestionType { get; init; } = "Text";

    public IReadOnlyList<CreateQuizOptionRequest> Options { get; init; } = [];
}

public sealed class CreateQuizOptionRequest
{
    [Required]
    [StringLength(1000)]
    public string Text { get; init; } = string.Empty;

    public bool IsCorrect { get; init; }
}