using ClassAttendance.Infrastructure.Persistence;
using ClassAttendance.Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using ClassAttendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassAttendance.Infrastructure.Repositories;

public sealed class LectureRepository(DataContext dbContext) : ILectureRepository
{
    public Task<Lecture?> GetById(int lectureId, CancellationToken cancellationToken = default)
    {
        return dbContext.Lectures
            .Include(lecture => lecture.Subject)
            .Include(lecture => lecture.Professor)
            .ThenInclude(professor => professor.User)
            .SingleOrDefaultAsync(lecture => lecture.Id == lectureId, cancellationToken);
    }

    public async Task<IReadOnlyList<Lecture>> GetFromStudent(
        int studentId,
        DateTime from,
        DateTime until,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Lectures
            .AsNoTracking()
            .Include(lecture => lecture.Subject)
            .Include(lecture => lecture.Professor)
            .ThenInclude(professor => professor.User)
            .Where(lecture => lecture.StartsAt >= from &&
            lecture.StartsAt < until &&
            dbContext.Enrollments.Any(enrollment =>
            enrollment.StudentId == studentId &&
            enrollment.SubjectId == lecture.SubjectId &&
            enrollment.Status == "Active"))
            .OrderBy(lecture => lecture.StartsAt)
            .ToListAsync(cancellationToken
            );
    }
}
