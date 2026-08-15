namespace ClassAttendance.Api.Contracts.Schedule;

public sealed record UpdateLectureRequest(
    DateTime StartsAt,
    DateTime EndsAt,
    string? Room

);
