using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Domain.Entities
{
    public class StudentDevice
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string DeviceFingerprint { get; set; } = null!;
        public bool isPrimary { get; set; }
        public DateTime RegisteredAt { get; set; }

        public Student Student { get; set; } = null!;

    }
}
