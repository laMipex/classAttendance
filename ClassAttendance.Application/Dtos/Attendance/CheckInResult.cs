using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Application.Dtos.Attendance;

public sealed class CheckInResult(
    int AttendanceId,
    int AttendanceSessionId,
    DateTime CheckInAt,
    string Status
);
