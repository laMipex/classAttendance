using ClassAttendance.Application.Dtos.Attendance;
using ClassAttendance.Application.Interfaces.Repositories;
using ClassAttendance.Application.Interfaces.Services;
using ClassAttendance.Domain.Entities;

namespace ClassAttendance.Infrastructure.Services;

public sealed class AttendanceService(
    IAttendanceRepository attendanceRepository,
    IStudentRepository studentRepository) : IAttendanceService
{
    public async Task<CheckInResult> CheckIn(
        int studentId,
        int attendanceSessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await attendanceRepository.GetSessionById(attendanceSessionId, cancellationToken)
            ?? throw new KeyNotFoundException("Attendance session was not found.");

        if (!await studentRepository.IsEnrolledInSubject(
                studentId,
                session.Lecture.SubjectId,
                cancellationToken))
        {
            throw new InvalidOperationException("Student is not enrolled in the subject.");
        }

        var now = DateTime.UtcNow;
        if (now < session.OpenFrom || now > session.OpenUntil)
        {
            throw new InvalidOperationException("Attendance session is not currently open.");
        }

        if (await attendanceRepository.HasCheckIn(attendanceSessionId, studentId, cancellationToken))
        {
            throw new InvalidOperationException("Student has already checked in for this session.");
        }

        var attendance = new Attendance
        {
            AttendanceSessionId = attendanceSessionId,
            StudentId = studentId,
            CheckInAt = now,
            Method = "Manual",
            Status = "Present"
        };

        await attendanceRepository.AddAsync(attendance, cancellationToken);

        return new CheckInResult(
            attendance.Id,
            attendance.AttendanceSessionId,
            attendance.CheckInAt,
            attendance.Status);
    }

    public async Task<IReadOnlyList<SubjectAttendanceDto>> GetForProfessorSubject(
        int professorId,
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        if (!await attendanceRepository.IsSubjectTaughtByProfessor(subjectId, professorId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Professor is not assigned to this subject.");
        }

        var attendances = await attendanceRepository.GetForSubject(subjectId, cancellationToken);
        return attendances.Select(attendance => new SubjectAttendanceDto(
            attendance.Id,
            attendance.AttendanceSessionId,
            attendance.AttendanceSession.LectureId,
            attendance.StudentId,
            attendance.Student.Index,
            $"{attendance.Student.User.FirstName} {attendance.Student.User.LastName}",
            attendance.CheckInAt,
            attendance.Status,
            attendance.Method)).ToList();
    }
}
