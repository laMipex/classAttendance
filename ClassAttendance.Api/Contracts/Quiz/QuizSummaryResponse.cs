namespace ClassAttendance.Api.Contracts.Quiz;

public sealed record QuizSummaryResponse(
    int Id,
    int LectureId,
    string Title,
    string SubjectName,
    DateTime StartsAt,
    DateTime EndsAt,
    int QuestionCount,
    bool IsVisible
);
