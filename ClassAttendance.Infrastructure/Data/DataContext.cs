using ClassAttendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using System.Reflection.Emit;

namespace ClassAttendance.Infrastructure.Persistence;

public class DataContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<StudyProgram> StudyPrograms => Set<StudyProgram>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Professor> Professors => Set<Professor>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<SubjectProfessor> SubjectProfessors => Set<SubjectProfessor>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Lecture> Lectures => Set<Lecture>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<StudentDevice> StudentDevices => Set<StudentDevice>();
    public DbSet<QuizSession> QuizSessions => Set<QuizSession>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizOption> QuizOptions => Set<QuizOption>();
    public DbSet<QuizResponse> QuizResponses => Set<QuizResponse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ----- User ------
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // ----- Student ------
        modelBuilder.Entity<Student>()
            .HasKey(u => u.UserId);

        modelBuilder.Entity<Student>()
            .HasOne(u => u.User)
            .WithOne()
            .HasForeignKey<Student>(u => u.UserId);

        modelBuilder.Entity<Student>()
            .HasOne(s => s.StudyProgram)
            .WithMany(sp => sp.Students)
            .HasForeignKey(s => s.StudyProgramId);

        // ----- Professor ------
        modelBuilder.Entity<Professor>()
            .HasKey(u => u.UserId);

        modelBuilder.Entity<Professor>()
            .HasOne(u => u.User)
            .WithOne()
            .HasForeignKey<Professor>(u => u.UserId);

        // ----- Subject ------
        modelBuilder.Entity<Subject>()
            .HasIndex(s => s.Code)
            .IsUnique();

        modelBuilder.Entity<SubjectProfessor>()
            .HasKey(sp => new { sp.SubjectId, sp.ProfessorId });

        modelBuilder.Entity<SubjectProfessor>()
            .HasOne(sp => sp.Subject)
            .WithMany()
            .HasForeignKey(sp => sp.SubjectId);

        modelBuilder.Entity<SubjectProfessor>()
            .HasOne(sp => sp.Professor)
            .WithMany()
            .HasForeignKey(sp => sp.ProfessorId);


        // ----- Enrollment ------
        modelBuilder.Entity<Enrollment>()
            .HasKey(e => new { e.StudentId, e.SubjectId });

        // ----- Lecture ------
        modelBuilder.Entity<Lecture>()
            .HasIndex(l => new { l.SubjectId, l.StartsAt });

        // ----- Attendance ------
        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.AttendanceSessionId, a.StudentId })
            .IsUnique();

        // ----- QuizResponse ------
        modelBuilder.Entity<QuizResponse>()
            .HasIndex(qr => new { qr.QuizSessionId, qr.QuestionId, qr.StudentId })
            .IsUnique();
    }
}
