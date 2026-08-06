using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ClassAttendance.Application.Dtos.Attendance;

public sealed record SubjectAttendanceDto(
    int AttendanceId,
    int AttendanceSessionId,
    int LectureId,
    int StudentId,
    string StudentIndex,
    string StudentName,
    DateTime CheckInAt,
    string Status,
    string Method
);
