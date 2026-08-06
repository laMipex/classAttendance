using System.ComponentModel.DataAnnotations;

namespace ClassAttendance.Api.Contracts.Attendance;

public class CheckInRequest
{
    [Range(1, int.MaxValue)]
    public int AttendanceSessionId { get; set; }
}