using ClassAttendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
            .HasForeignKey<Student>(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Student>()
            .HasOne(s => s.StudyProgram)
            .WithMany(sp => sp.Students)
            .HasForeignKey(s => s.StudyProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        // ----- Professor ------
        modelBuilder.Entity<Professor>()
            .HasKey(u => u.UserId);

        modelBuilder.Entity<Professor>()
            .HasOne(u => u.User)
            .WithOne()
            .HasForeignKey<Professor>(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----- Subject ------
        modelBuilder.Entity<Subject>()
            .HasIndex(s => s.Code)
            .IsUnique();

        modelBuilder.Entity<SubjectProfessor>()
            .HasKey(sp => new { sp.SubjectId, sp.ProfessorId });

        modelBuilder.Entity<SubjectProfessor>()
            .HasOne(sp => sp.Subject)
            .WithMany()
            .HasForeignKey(sp => sp.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SubjectProfessor>()
            .HasOne(sp => sp.Professor)
            .WithMany()
            .HasForeignKey(sp => sp.ProfessorId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----- Enrollment ------
        modelBuilder.Entity<Enrollment>()
            .HasKey(e => new { e.StudentId, e.SubjectId });

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Subject)
            .WithMany()
            .HasForeignKey(e => e.SubjectId)
            .OnDelete(DeleteBehavior.NoAction);

        // ----- Lecture ------
        modelBuilder.Entity<Lecture>()
        .HasIndex(l => new { l.SubjectId, l.StartsAt });

        modelBuilder.Entity<Lecture>()
            .HasOne(l => l.Subject)
            .WithMany()
            .HasForeignKey(l => l.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Lecture>()
            .HasOne(l => l.Professor)
            .WithMany()
            .HasForeignKey(l => l.ProfessorId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----- AttendanceSession ------
        modelBuilder.Entity<AttendanceSession>()
            .HasOne(a => a.Lecture)
            .WithMany()
            .HasForeignKey(a => a.LectureId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----- Attendance ------
        modelBuilder.Entity<Attendance>()
        .HasIndex(a => new { a.AttendanceSessionId, a.StudentId })
        .IsUnique();

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.AttendanceSession)
            .WithMany()
            .HasForeignKey(a => a.AttendanceSessionId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Student)
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        // ----- StudentDevice ------
        modelBuilder.Entity<StudentDevice>()
            .HasOne(sd => sd.Student)
            .WithMany()
            .HasForeignKey(sd => sd.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----- QuizSession ------
        modelBuilder.Entity<QuizSession>()
            .HasOne(qs => qs.Lecture)
            .WithMany()
            .HasForeignKey(qs => qs.LectureId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----- QuizQuestion ------
        modelBuilder.Entity<QuizQuestion>()
            .HasOne(qq => qq.QuizSession)
            .WithMany()
            .HasForeignKey(qq => qq.QuizSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----- QuizOption ------
        modelBuilder.Entity<QuizOption>()
            .HasOne(qo => qo.Question)
            .WithMany()
            .HasForeignKey(qo => qo.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----- QuizResponse ------
        modelBuilder.Entity<QuizResponse>()
            .HasIndex(qr => new { qr.QuizSessionId, qr.QuestionId, qr.StudentId })
            .IsUnique();

        modelBuilder.Entity<QuizResponse>()
            .HasOne(qr => qr.QuizSession)
            .WithMany()
            .HasForeignKey(qr => qr.QuizSessionId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<QuizResponse>()
            .HasOne(qr => qr.Question)
            .WithMany()
            .HasForeignKey(qr => qr.QuestionId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<QuizResponse>()
            .HasOne(qr => qr.Student)
            .WithMany()
            .HasForeignKey(qr => qr.StudentId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
