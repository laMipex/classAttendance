using ClassAttendance.Application.Dtos.Schedule;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Application.Interfaces.Services;

public interface IScheduleService
{
    Task<IReadOnlyList<ScheduleLectureDto>> GetStudentSchedule(
        int studentId,
        DateTime from,
        DateTime until,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleLectureDto>> GetProfessorSchedule(
        int professorId,
        DateTime from,
        DateTime until,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateLecture(
        int professorId,
        int lectureId,
        DateTime startsAt,
        DateTime endsAt,
        string? rooms,
        CancellationToken cancellationToken = default);
}
