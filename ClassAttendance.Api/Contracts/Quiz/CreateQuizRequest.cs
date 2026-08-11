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
}