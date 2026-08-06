namespace ClassAttendance.Application.Dtos.Schedule;

public sealed record ScheduleLectureDto(
    int Id,
    int SubjectId,
    string SubjectCode,
    string SubjectName,
    string ProfessorName,
    DateTime StartsAt,
    DateTime EndsAt,
    string? Room
);
