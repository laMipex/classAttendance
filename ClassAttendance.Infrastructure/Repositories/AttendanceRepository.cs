using ClassAttendance.Application.Interfaces.Repositories;
using ClassAttendance.Domain.Entities;
using ClassAttendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Infrastructure.Repositories;


public sealed class AttendanceRepository(DataContext dbContext) : IAttendanceRepository
{
    public Task<AttendanceSession?> GetSessionById(
        int attendanceSessionId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.AttendanceSessions
            .Include(session => session.Lecture)
                .ThenInclude(lecture => lecture.Subject)
            .SingleOrDefaultAsync(session => session.Id == attendanceSessionId, cancellationToken);
    }

    public Task<bool> HasCheckIn(
       int attendanceSessionId,
       int studentId,
       CancellationToken cancellationToken = default)
    {
        return dbContext.Attendances.AnyAsync(
            attendance => attendance.AttendanceSessionId == attendanceSessionId
                && attendance.StudentId == studentId,
            cancellationToken);
    }

    public Task<Attendance?> GetCheckIn(
        int attendanceSessionId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Attendances
            .AsNoTracking()
            .SingleOrDefaultAsync(
                attendance => attendance.AttendanceSessionId == attendanceSessionId
                    && attendance.StudentId == studentId,
                cancellationToken);
    }

    public async Task AddAsync(Attendance attendance, CancellationToken cancellationToken = default)
    {
        await dbContext.Attendances.AddAsync(attendance, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException?.Message.Contains(
                "IX_Attendances_AttendanceSessionId_StudentId",
                StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new InvalidOperationException(
                "Student has already checked in for this session.",
                exception);
        }
    }
    public async Task<IReadOnlyList<Attendance>> GetForSubject(
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Attendances
            .AsNoTracking()
            .Include(attendance => attendance.Student)
                .ThenInclude(student => student.User)
            .Include(attendance => attendance.AttendanceSession)
                .ThenInclude(session => session.Lecture)
                    .ThenInclude(lecture => lecture.Subject)
            .Where(attendance => attendance.AttendanceSession.Lecture.SubjectId == subjectId)
            .OrderByDescending(attendance => attendance.CheckInAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsSubjectTaughtByProfessor(
        int subjectId,
        int professorId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.SubjectProfessors.AnyAsync(
            assignment => assignment.SubjectId == subjectId
                && assignment.ProfessorId == professorId,
            cancellationToken);
    }
}