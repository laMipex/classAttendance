namespace ClassAttendance.Api.Contracts.Quiz;

public sealed record QuizLectureResponse(
    int Id,
    string SubjectName,
    DateTime StartsAt,
    DateTime EndsAt,
    string? Room
);
