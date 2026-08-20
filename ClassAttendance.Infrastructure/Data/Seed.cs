using ClassAttendance.Domain.Entities;
using ClassAttendance.Infrastructure.Persistence;
using ClassAttendance.Infrastructure.Security;
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
            var alreadySeeded = await db.Users.AnyAsync(u => u.Email == "ivan.ivanovic@classattendance.com", ct);
            if (alreadySeeded)
            {
                await EnsureAdditionalDemoDataAsync(db, ct);
                return;
            }
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
                Password = PasswordHasher.HashPassword("Prof123!"),
                Role = "Professor",
                IsActive = true
            };
            var professor2User = new User
            {
                FirstName = "Milica",
                LastName = "Jovanović",
                Email = "milica.jovanovic@classattendance.com",
                Password = PasswordHasher.HashPassword("Prof123!"),
                Role = "Professor",
                IsActive = true
            };
            var student1User = new User
            {
                FirstName = "Marko",
                LastName = "Marković",
                Email = "marko.markovic@classattendance.com",
                Password = PasswordHasher.HashPassword("Student123!"),
                Role = "Student",
                IsActive = true
            };
            var student2User = new User
            {
                FirstName = "Ana",
                LastName = "Nikolić",
                Email = "ana.nikolic@classattendance.com",
                Password = PasswordHasher.HashPassword("Student123!"),
                Role = "Student",
                IsActive = true
            };
            db.Users.AddRange(professorUser, professor2User, student1User, student2User);
            await db.SaveChangesAsync(ct);
            var professor = new Professor
            {
                UserId = professorUser.Id,
                EmployeeCode = "EMP-0001"
            };
            var professor2 = new Professor
            {
                UserId = professor2User.Id,
                EmployeeCode = "EMP-0002"
            };
            var student1 = new Student
            {
                UserId = student1User.Id,
                Index = "26122083",
                StudyProgramId = studyProgram.Id
            };
            var student2 = new Student
            {
                UserId = student2User.Id,
                Index = "26122035",
                StudyProgramId = studyProgram.Id
            };
            db.Professors.AddRange(professor, professor2);
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
            var subject3 = new Subject
            {
                Code = "WEB1",
                Name = "Web programiranje",
                ETCS = 6,
                Semester = 2
            };
            db.Subjects.AddRange(subject1, subject2, subject3);
            await db.SaveChangesAsync(ct);
            db.SubjectProfessors.AddRange(
                new SubjectProfessor { SubjectId = subject1.Id, ProfessorId = professor.UserId },
                new SubjectProfessor { SubjectId = subject2.Id, ProfessorId = professor.UserId },
                new SubjectProfessor { SubjectId = subject3.Id, ProfessorId = professor2.UserId }
            );
            db.Enrollments.AddRange(
                new Enrollment { StudentId = student1.UserId, SubjectId = subject1.Id, AcademicYear = "2026/2027", Status = "Active" },
                new Enrollment { StudentId = student1.UserId, SubjectId = subject2.Id, AcademicYear = "2026/2027", Status = "Active" },
                new Enrollment { StudentId = student2.UserId, SubjectId = subject1.Id, AcademicYear = "2026/2027", Status = "Active" },
                new Enrollment { StudentId = student2.UserId, SubjectId = subject2.Id, AcademicYear = "2026/2027", Status = "Active" },
                new Enrollment { StudentId = student1.UserId, SubjectId = subject3.Id, AcademicYear = "2026/2027", Status = "Active" },
                new Enrollment { StudentId = student2.UserId, SubjectId = subject3.Id, AcademicYear = "2026/2027", Status = "Active" }
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
            var lecture3 = new Lecture
            {
                SubjectId = subject1.Id,
                ProfessorId = professor.UserId,
                StartsAt = now.AddDays(2).Date.AddHours(8),
                EndsAt = now.AddDays(2).Date.AddHours(10),
                Rooms = "A1"
            };
            var lecture4 = new Lecture
            {
                SubjectId = subject3.Id,
                ProfessorId = professor2.UserId,
                StartsAt = now.AddDays(3).Date.AddHours(14),
                EndsAt = now.AddDays(3).Date.AddHours(16),
                Rooms = "B2"
            };
            db.Lectures.AddRange(lecture1, lecture2, lecture3, lecture4);
            await db.SaveChangesAsync(ct);
            var attendanceSession = new AttendanceSession
            {
                LectureId = lecture1.Id,
                OpenFrom = now.AddMinutes(-15),
                OpenUntil = now.AddMinutes(15),
                WifiRequired = false
            };
            var futureAttendanceSession = new AttendanceSession
            {
                LectureId = lecture2.Id,
                OpenFrom = lecture2.StartsAt.AddMinutes(-15),
                OpenUntil = lecture2.StartsAt.AddMinutes(15),
                WifiRequired = false
            };
            var laterAttendanceSession = new AttendanceSession
            {
                LectureId = lecture3.Id,
                OpenFrom = lecture3.StartsAt.AddMinutes(-15),
                OpenUntil = lecture3.StartsAt.AddMinutes(15),
                WifiRequired = false
            };
            var fourthAttendanceSession = new AttendanceSession
            {
                LectureId = lecture4.Id,
                OpenFrom = lecture4.StartsAt.AddMinutes(-15),
                OpenUntil = lecture4.StartsAt.AddMinutes(15),
                WifiRequired = false
            };
            db.AttendanceSessions.AddRange(
                attendanceSession,
                futureAttendanceSession,
                laterAttendanceSession,
                fourthAttendanceSession);
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

        private static async Task EnsureAdditionalDemoDataAsync(DataContext db, CancellationToken ct)
        {
            if (await db.Subjects.AnyAsync(subject => subject.Code == "WEB1", ct))
            {
                return;
            }

            var studyProgram = await db.StudyPrograms.FirstAsync(ct);
            var professorUser = new User
            {
                FirstName = "Milica",
                LastName = "Jovanović",
                Email = "milica.jovanovic@classattendance.com",
                Password = PasswordHasher.HashPassword("Prof123!"),
                Role = "Professor",
                IsActive = true
            };
            db.Users.Add(professorUser);
            await db.SaveChangesAsync(ct);

            db.Professors.Add(new Professor { UserId = professorUser.Id, EmployeeCode = "EMP-0002" });
            var subject = new Subject
            {
                Code = "WEB1",
                Name = "Web programiranje",
                ETCS = 6,
                Semester = 2
            };
            db.Subjects.Add(subject);
            await db.SaveChangesAsync(ct);

            db.SubjectProfessors.Add(new SubjectProfessor
            {
                SubjectId = subject.Id,
                ProfessorId = professorUser.Id
            });
            foreach (var student in await db.Students.ToListAsync(ct))
            {
                db.Enrollments.Add(new Enrollment
                {
                    StudentId = student.UserId,
                    SubjectId = subject.Id,
                    AcademicYear = "2026/2027",
                    Status = "Active"
                });
            }

            var now = DateTime.UtcNow;
            var lecture = new Lecture
            {
                SubjectId = subject.Id,
                ProfessorId = professorUser.Id,
                StartsAt = now.AddDays(3).Date.AddHours(14),
                EndsAt = now.AddDays(3).Date.AddHours(16),
                Rooms = "B2"
            };
            db.Lectures.Add(lecture);
            await db.SaveChangesAsync(ct);
            db.AttendanceSessions.Add(new AttendanceSession
            {
                LectureId = lecture.Id,
                OpenFrom = lecture.StartsAt.AddMinutes(-15),
                OpenUntil = lecture.StartsAt.AddMinutes(15),
                WifiRequired = false
            });
            await db.SaveChangesAsync(ct);
        }

    }
}