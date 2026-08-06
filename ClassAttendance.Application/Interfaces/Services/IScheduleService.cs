using ClassAttendance.Application.Dtos.Schedule;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Application.Interfaces.Services;

public interface IScheduleService
{
    Task<IReadOnlyList<ScheduleLectureDto>> GetStudentSchedule(
        int stundentId,
        DateTime from,
        DateTime until,
        CancellationToken cancellationToken = default);
}
