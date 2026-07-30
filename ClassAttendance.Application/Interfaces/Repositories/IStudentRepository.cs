using ClassAttendance.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Application.Interfaces.Repositories
{
    public interface IStudentRepository
    {
        Task<Student?> GetByUserId(int userId, CancellationToken cancellationToken = default);

        Task<bool> IsEnrolledInSubject(
            int studnetId,
            int subjectId,
            CancellationToken cancellationToken = default);
    }
}
