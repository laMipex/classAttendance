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
            .Include(lecture => lecture.AttendanceSessions)
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

    public async Task<IReadOnlyList<Lecture>> GetForProfessor(
        int professorId,
        DateTime from,
        DateTime until,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Lectures
            .AsNoTracking()
            .Include(lecture => lecture.Subject)
            .Include(lecture => lecture.Professor)
                .ThenInclude(professor => professor.User)
            .Include(lecture => lecture.AttendanceSessions)
            .Where(lecture => lecture.ProfessorId == professorId
                && lecture.StartsAt >= from
                && lecture.StartsAt < until)
            .OrderBy(lecture => lecture.StartsAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateSchedule(
        int lectureId,
        int professorId,
        DateTime startsAt,
        DateTime endsAt,
        string? rooms,
        CancellationToken cancellationToken = default)
    {
        var lecture = await dbContext.Lectures
            .Include(item => item.AttendanceSessions)
            .SingleOrDefaultAsync(
                item => item.Id == lectureId && item.ProfessorId == professorId,
                cancellationToken);

        if (lecture is null)
        {
            return false;
        }

        lecture.StartsAt = startsAt;
        lecture.EndsAt = endsAt;
        lecture.Rooms = rooms;

        foreach (var session in lecture.AttendanceSessions)
        {
            session.OpenFrom = startsAt.AddMinutes(-15);
            session.OpenUntil = startsAt.AddMinutes(15);
        }

        var quizzes = await dbContext.QuizSessions
            .Where(quiz => quiz.LectureId == lectureId)
            .ToListAsync(cancellationToken);
        foreach (var quiz in quizzes)
        {
            quiz.StartsAt = startsAt;
            quiz.EndsAt = endsAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
