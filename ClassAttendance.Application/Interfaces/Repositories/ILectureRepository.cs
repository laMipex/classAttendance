using ClassAttendance.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Application.Interfaces.Repositories
{
    public interface ILectureRepository
    {
        Task<Lecture?> GetById(int lectureId, CancellationToken cancellationToken = default);
        
        Task<IReadOnlyList<Lecture>> GetFromStudent(
            int studentId,
            DateTime from,
            DateTime until,
            CancellationToken cancellationToken = default);
    }
}
