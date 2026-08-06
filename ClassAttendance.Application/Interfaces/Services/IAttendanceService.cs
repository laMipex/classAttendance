using ClassAttendance.Application.Dtos.Attendance;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Application.Interfaces.Services;

public interface IAttendanceService
{
    Task<CheckInResult> CheckIn (
        int studentId,
        int attendanceSessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubjectAttendanceDto>> GetForProfessorSubject(
        int professorId,
        int subjectId,
        CancellationToken cancellationToken= default);
}
