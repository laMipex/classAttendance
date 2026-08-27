using ClassAttendance.Domain.Entities;
using ClassAttendance.Infrastructure.Services;
using ClassAttendance.Application.Interfaces.Repositories;

namespace ClassAttendance.Infrastructure.Tests;

public sealed class AttendanceServiceTests
{
    [Fact]
    public async Task CheckIn_RejectsStudentOutsideOpenWindow()
    {
        var repository = new FakeAttendanceRepository
        {
            Session = CreateSession(DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow.AddMinutes(-15))
        };
        var students = new FakeStudentRepository { IsEnrolled = true };
        var service = new AttendanceService(repository, students);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CheckIn(7, 12));

        Assert.Equal("Attendance session is not currently open.", exception.Message);
        Assert.False(repository.AttendanceAdded);
    }

    [Fact]
    public async Task CheckIn_RejectsStudentNotEnrolledInSubject()
    {
        var repository = new FakeAttendanceRepository
        {
            Session = CreateSession(DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(5))
        };
        var students = new FakeStudentRepository { IsEnrolled = false };
        var service = new AttendanceService(repository, students);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CheckIn(7, 12));

        Assert.Equal("Student is not enrolled in the subject.", exception.Message);
        Assert.False(repository.AttendanceAdded);
    }

    [Fact]
    public async Task CheckIn_ReturnsExistingAttendanceWithoutCreatingDuplicate()
    {
        var existing = new Attendance
        {
            Id = 99,
            AttendanceSessionId = 12,
            StudentId = 7,
            CheckInAt = DateTime.UtcNow.AddMinutes(-1),
            Status = "Present",
            Method = "Manual"
        };
        var repository = new FakeAttendanceRepository
        {
            Session = CreateSession(DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(5)),
            ExistingAttendance = existing
        };
        var service = new AttendanceService(repository, new FakeStudentRepository { IsEnrolled = true });

        var result = await service.CheckIn(7, 12);

        Assert.Equal(existing.Id, result.AttendanceId);
        Assert.False(repository.AttendanceAdded);
    }

    private static AttendanceSession CreateSession(DateTime openFrom, DateTime openUntil) =>
        new()
        {
            Id = 12,
            OpenFrom = openFrom,
            OpenUntil = openUntil,
            Lecture = new Lecture { SubjectId = 42 }
        };

    private sealed class FakeAttendanceRepository : IAttendanceRepository
    {
        public AttendanceSession? Session { get; init; }
        public Attendance? ExistingAttendance { get; init; }
        public bool AttendanceAdded { get; private set; }

        public Task<AttendanceSession?> GetSessionById(int attendanceSessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Session);

        public Task<Attendance?> GetCheckIn(int attendanceSessionId, int studentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExistingAttendance);

        public Task<bool> HasCheckIn(int attendanceSessionId, int studentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExistingAttendance is not null);

        public Task AddAsync(Attendance attendance, CancellationToken cancellationToken = default)
        {
            AttendanceAdded = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Attendance>> GetForSubject(int subjectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Attendance>>([]);

        public Task<bool> IsSubjectTaughtByProfessor(int subjectId, int professorId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class FakeStudentRepository : IStudentRepository
    {
        public bool IsEnrolled { get; init; }

        public Task<Student?> GetByUserId(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Student?>(null);

        public Task<bool> IsEnrolledInSubject(int studentId, int subjectId, CancellationToken cancellationToken = default) =>
            Task.FromResult(IsEnrolled);
    }
}
