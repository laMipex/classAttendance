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

    public async Task AddAsync(Attendance attendance, CancellationToken cancellationToken = default)
    {
        await dbContext.Attendances.AddAsync(attendance, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
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
}