namespace ClassAttendance.Application.Dtos.Schedule;

public sealed record ScheduleAttendanceSessionDto(
    int Id,
    DateTime OpenFrom,
    DateTime OpenUntil
);
