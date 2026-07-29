using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Domain.Entities
{
    public class AttendanceSession
    {
        public int Id { get; set; }
        public int LectureId { get; set; }

        public DateTime OpenFrom { get; set; }
        public DateTime OpenUntil { get; set; }

        public string? QrTokenHash { get; set; }
        public bool WifiRequired { get; set; }

        public Lecture Lecture { get; set; } = null!;

    }
}
