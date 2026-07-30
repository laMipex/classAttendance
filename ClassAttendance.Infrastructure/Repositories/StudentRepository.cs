using ClassAttendance.Infrastructure.Persistence;
using ClassAttendance.Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using ClassAttendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassAttendance.Infrastructure.Repositories;

public sealed class StudentRepository(DataContext dbContext) : IStudentRepository
{
    public Task<Student?> GetByUserId(int userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Students
            .Include(student => student.User)
            .Include(student => student.StudyProgram)
            .SingleOrDefaultAsync(student => student.UserId == userId, cancellationToken);
    }

    public Task<bool> IsEnrolledInSubject (
        int stundentId,
        int subjectId,  
        CancellationToken cancellationToken = default)
    {
        return dbContext.Enrollments.AnyAsync(
            enrollment => enrollment.SubjectId == subjectId &&
            enrollment.SubjectId == stundentId &&
            enrollment.Status == "Active", cancellationToken
            );
    }
}
