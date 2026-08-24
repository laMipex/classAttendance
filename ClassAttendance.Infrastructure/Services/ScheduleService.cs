using ClassAttendance.Application.Dtos.Schedule;
using ClassAttendance.Application.Interfaces.Repositories;
using ClassAttendance.Application.Interfaces.Services;

namespace ClassAttendance.Infrastructure.Services;

public sealed class ScheduleService(ILectureRepository lectureRepository) : IScheduleService
{
    public async Task<IReadOnlyList<ScheduleLectureDto>> GetStudentSchedule(
        int studentId,
        DateTime from,
        DateTime until,
        CancellationToken cancellationToken = default)
    {
        var lectures = await lectureRepository.GetFromStudent(studentId, from, until, cancellationToken);

        return lectures.Select(lecture => new ScheduleLectureDto(
            lecture.Id,
            lecture.SubjectId,
            lecture.Subject.Code,
            lecture.Subject.Name,
            $"{lecture.Professor.User.FirstName} {lecture.Professor.User.LastName}",
            lecture.StartsAt,
            lecture.EndsAt,
            lecture.Rooms,
            lecture.AttendanceSessions
                .OrderBy(session => session.OpenFrom)
                .Select(session => new ScheduleAttendanceSessionDto(
                    session.Id,
                    session.OpenFrom,
                    session.OpenUntil))
                .ToList())).ToList();
    }

    public async Task<IReadOnlyList<ScheduleLectureDto>> GetProfessorSchedule(
        int professorId,
        DateTime from,
        DateTime until,
        CancellationToken cancellationToken = default)
    {
        var lectures = await lectureRepository.GetForProfessor(professorId, from, until, cancellationToken);
        return lectures.Select(ToDto).ToList();
    }

    public Task<bool> UpdateLecture(
        int professorId,
        int lectureId,
        DateTime startsAt,
        DateTime endsAt,
        string? rooms,
        CancellationToken cancellationToken = default)
    {
        if (endsAt <= startsAt)
        {
            throw new ArgumentException("Lecture end must be after its start.");
        }

        return lectureRepository.UpdateSchedule(
                lectureId, professorId, startsAt, endsAt, rooms, cancellationToken);
    }

    private static ScheduleLectureDto ToDto(ClassAttendance.Domain.Entities.Lecture lecture) =>
        new(
                lecture.Id,
                lecture.SubjectId,
                lecture.Subject.Code,
                lecture.Subject.Name,
                $"{lecture.Professor.User.FirstName} {lecture.Professor.User.LastName}",
                lecture.StartsAt,
                lecture.EndsAt,
                lecture.Rooms,
                lecture.AttendanceSessions
                    .OrderBy(session => session.OpenFrom)
                    .Select(session => new ScheduleAttendanceSessionDto(
                        session.Id, session.OpenFrom, session.OpenUntil))
                    .ToList());
}
