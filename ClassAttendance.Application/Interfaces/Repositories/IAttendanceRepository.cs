using ClassAttendance.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Application.Interfaces.Repositories
{
    public interface IAttendanceRepository
    {
        Task<AttendanceSession?> GetSessionById(int attendanceSessionId, CancellationToken cancellationToken = default);

        Task<bool> HasCheckIn(
            int attendanceSessionId,
            int studentId,
            CancellationToken cancellationToken = default);

        Task<Attendance?> GetCheckIn(
            int attendanceSessionId,
            int studentId,
            CancellationToken cancellationToken = default);

        Task AddAsync(Attendance attendance, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Attendance>> GetForSubject(int subjectId, CancellationToken cancellationToken = default);

        Task<bool> IsSubjectTaughtByProfessor(
            int subjectId,
            int professorId,
            CancellationToken cancellationToken = default);
    }
}
