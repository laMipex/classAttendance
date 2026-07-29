using ClassAttendance.Domain.Entities;
using ClassAttendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassAttendance.Infrastructure.Data
{
    public static class Seed
    {
        public static async Task SeedAsync(DataContext db, CancellationToken ct = default)
        {
            await db.Database.MigrateAsync(ct);

            var alreadySeeded = await db.Users.AnyAsync(u => u.Email == "prof.demo@classattendance.local", ct);
            if (alreadySeeded) return;

            var now = DateTime.UtcNow;

            var studyProgram = new StudyProgram
            {
                Name = "Informatika",
                Year = 1
            };
            db.StudyPrograms.Add(studyProgram);
            await db.SaveChangesAsync(ct);

            var professorUser = new User
            {
                FirstName = "Ivan",
                LastName = "Ivanović",
                Email = "ivan.ivanovic@classattendance.com",
                Password = "Prof123!", 
                Role = "Professor",
                IsActive = true
            };

            var student1User = new User
            {
                FirstName = "Marko",
                LastName = "Marković",
                Email = "marko.markovic@classattendance.com",
                Password = "Student123!",
                Role = "Student",
                IsActive = true
            };

            var student2User = new User
            {
                FirstName = "Ana",
                LastName = "Nikolić",
                Email = "ana.nikolic@classattendance.com",
                Password = "Student123!",
                Role = "Student",
                IsActive = true
            };

            db.Users.AddRange(professorUser, student1User, student2User);
            await db.SaveChangesAsync(ct);

            var professor = new Professor
            {
                UserId = professorUser.id,
                EmployeeCode = "EMP-0001"
            };

            var student1 = new Student
            {
                UserId = student1User.id,
                Index = "RA-001/26",
                StudyProgramId = studyProgram.Id
            };

            var student2 = new Student
            {
                UserId = student2User.id,
                Index = "RA-002/26",
                StudyProgramId = studyProgram.Id
            };

            db.Professors.Add(professor);
            db.Students.AddRange(student1, student2);
            await db.SaveChangesAsync(ct);

            var subject1 = new Subject
            {
                Code = "OOP1",
                Name = "Objektno orijentisano programiranje",
                ETCS = 6,
                Semester = 2
            };

            var subject2 = new Subject
            {
                Code = "DB1",
                Name = "Baze podataka 1",
                ETCS = 6,
                Semester = 2
            };

            db.Subjects.AddRange(subject1, subject2);
            await db.SaveChangesAsync(ct);

            db.SubjectProfessors.AddRange(
                new SubjectProfessor { SubjectId = subject1.Id, ProfessorId = professor.UserId },
                new SubjectProfessor { SubjectId = subject2.Id, ProfessorId = professor.UserId }
            );

            db.Enrollments.AddRange(
                new Enrollment { StudentId = student1.UserId, SubjectId = subject1.Id, AcademicYear = "2026/2027", Status = "Active" },
                new Enrollment { StudentId = student1.UserId, SubjectId = subject2.Id, AcademicYear = "2026/2027", Status = "Active" },
                new Enrollment { StudentId = student2.UserId, SubjectId = subject1.Id, AcademicYear = "2026/2027", Status = "Active" },
                new Enrollment { StudentId = student2.UserId, SubjectId = subject2.Id, AcademicYear = "2026/2027", Status = "Active" }
            );

            var lecture1 = new Lecture
            {
                SubjectId = subject1.Id,
                ProfessorId = professor.UserId,
                StartsAt = now.AddHours(-1),
                EndsAt = now.AddHours(1),
                Rooms = "A1"
            };

            var lecture2 = new Lecture
            {
                SubjectId = subject2.Id,
                ProfessorId = professor.UserId,
                StartsAt = now.AddDays(1).Date.AddHours(10),
                EndsAt = now.AddDays(1).Date.AddHours(12),
                Rooms = "A2"
            };

            db.Lectures.AddRange(lecture1, lecture2);
            await db.SaveChangesAsync(ct);

            var attendanceSession = new AttendanceSession
            {
                LectureId = lecture1.Id,
                OpenFrom = now.AddMinutes(-15),
                OpenUntil = now.AddMinutes(15),
                WifiRequired = false
            };

            db.AttendanceSessions.Add(attendanceSession);
            await db.SaveChangesAsync(ct);

            db.Attendances.Add(new Attendance
            {
                AttendanceSessionId = attendanceSession.Id,
                StudentId = student1.UserId,
                CheckInAt = now.AddMinutes(-5),
                Method = "Manual",
                Status = "Present"
            });

            await db.SaveChangesAsync(ct);
        }
    }
}
