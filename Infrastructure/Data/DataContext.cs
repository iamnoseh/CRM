using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class DataContext(DbContextOptions<DataContext> options) : IdentityDbContext<User, IdentityRole<int>, int> (options)
{
    public DbSet<Course> Courses { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Mentor> Mentors { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<StudentGroup> StudentGroups { get; set; }
    public DbSet<MentorGroup> MentorGroups { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Center> Centers { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<StudentPerformance> StudentPerformances { get; set; }
    public DbSet<StudentStatistics> StudentStatistics { get; set; }
    public DbSet<MonthlySummary> MonthlySummaries { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Course vs Group
        modelBuilder.Entity<Group>()
            .HasOne(g => g.Course)
            .WithMany(c => c.Groups)
            .HasForeignKey(g => g.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        //  Group vs Mentor
        modelBuilder.Entity<Group>()
            .HasOne(g => g.Mentor)
            .WithMany(m => m.Groups)
            .HasForeignKey(g => g.MentorId)
            .OnDelete(DeleteBehavior.Restrict); 

        //  Lesson vs Group
        modelBuilder.Entity<Lesson>()
            .HasOne(l => l.Group)
            .WithMany(g => g.Lessons)
            .HasForeignKey(l => l.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        //  Grade vs Lesson, Student, Group
        modelBuilder.Entity<Grade>()
            .HasOne(g => g.Lesson)
            .WithMany(l => l.Grades)
            .HasForeignKey(g => g.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Grade>()
            .HasOne(g => g.Student)
            .WithMany(s => s.Grades)
            .HasForeignKey(g => g.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Grade>()
            .HasOne(g => g.Group)
            .WithMany()
            .HasForeignKey(g => g.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Exam связь с Group
        modelBuilder.Entity<Exam>()
            .HasOne(e => e.Group)
            .WithMany(g => g.Exams)
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Связь Grade с Exam 
        modelBuilder.Entity<Grade>()
            .HasOne(g => g.Exam)
            .WithMany(e => e.Grades)
            .HasForeignKey(g => g.ExamId)
            .OnDelete(DeleteBehavior.SetNull);

        //  Attendance vs Lesson, Student, Group
        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Lesson)
            .WithMany(l => l.Attendances)
            .HasForeignKey(a => a.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Student)
            .WithMany(s => s.Attendances)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Group)
            .WithMany()
            .HasForeignKey(a => a.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Comment vs Student, Group, Lesson
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Student)
            .WithMany(s => s.Comments)
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Group)
            .WithMany(g => g.Comments)
            .HasForeignKey(c => c.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Lesson)
            .WithMany(l => l.Comments)
            .HasForeignKey(c => c.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        

        //  StudentGroup (many to many)
        modelBuilder.Entity<StudentGroup>()
            .HasKey(sg => new { sg.StudentId, sg.GroupId }); 

        modelBuilder.Entity<StudentGroup>()
            .HasOne(sg => sg.Student)
            .WithMany(s => s.StudentGroups)
            .HasForeignKey(sg => sg.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StudentGroup>()
            .HasOne(sg => sg.Group)
            .WithMany(g => g.StudentGroups)
            .HasForeignKey(sg => sg.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StudentGroup>()
            .HasIndex(sg => sg.StudentId);

        modelBuilder.Entity<StudentGroup>()
            .HasIndex(sg => sg.GroupId);

        modelBuilder.Entity<MentorGroup>()
            .HasOne(mg => mg.Mentor)
            .WithMany()
            .HasForeignKey(mg => mg.MentorId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<MentorGroup>()
            .HasOne(mg => mg.Group)
            .WithMany()
            .HasForeignKey(mg => mg.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<MentorGroup>()
            .HasIndex(mg => mg.MentorId);
            
        modelBuilder.Entity<MentorGroup>()
            .HasIndex(mg => mg.GroupId);

        //  User vs Student, Mentor (як ба як)
        modelBuilder.Entity<Student>()
            .HasOne(s => s.User)
            .WithOne(u => u.StudentProfile)
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Mentor>()
            .HasOne(m => m.User)
            .WithOne(u => u.MentorProfile)
            .HasForeignKey<Mentor>(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Center vs User, Student, Mentor
        modelBuilder.Entity<User>()
            .HasOne(u => u.Center)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.CenterId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<Student>()
            .HasOne(s => s.Center)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.CenterId)
            .OnDelete(DeleteBehavior.SetNull);
            
        // Payment vs Student, Group, Center
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Student)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Group)
            .WithMany()
            .HasForeignKey(p => p.GroupId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Center)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.CenterId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.StudentId);
            
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.GroupId);
            
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.CenterId);
            
        modelBuilder.Entity<Payment>()
            .HasIndex(p => new { p.Month, p.Year });
            
        modelBuilder.Entity<StudentPerformance>()
            .HasOne(sp => sp.Student)
            .WithMany()
            .HasForeignKey(sp => sp.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<StudentPerformance>()
            .HasOne(sp => sp.Group)
            .WithMany()
            .HasForeignKey(sp => sp.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<StudentStatistics>()
            .HasOne(ss => ss.Student)
            .WithMany()
            .HasForeignKey(ss => ss.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<StudentStatistics>()
            .HasOne(ss => ss.Group)
            .WithMany()
            .HasForeignKey(ss => ss.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<MonthlySummary>()
            .HasOne(ms => ms.Center)
            .WithMany(c => c.MonthlySummaries)
            .HasForeignKey(ms => ms.CenterId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<NotificationLog>()
            .HasOne(nl => nl.Student)
            .WithMany()
            .HasForeignKey(nl => nl.StudentId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<NotificationLog>()
            .HasOne(nl => nl.Group)
            .WithMany()
            .HasForeignKey(nl => nl.GroupId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<NotificationLog>()
            .HasOne(nl => nl.Center)
            .WithMany()
            .HasForeignKey(nl => nl.CenterId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Group>()
            .HasIndex(g => g.CourseId);

        modelBuilder.Entity<Group>()
            .HasIndex(g => g.MentorId);

        modelBuilder.Entity<Lesson>()
            .HasIndex(l => l.GroupId);

        modelBuilder.Entity<Grade>()
            .HasIndex(g => g.LessonId);

        modelBuilder.Entity<Grade>()
            .HasIndex(g => g.StudentId);

        modelBuilder.Entity<Grade>()
            .HasIndex(g => g.GroupId);

        modelBuilder.Entity<Exam>()
            .HasIndex(e => e.GroupId);

        modelBuilder.Entity<Attendance>()
            .HasIndex(a => a.LessonId);

        modelBuilder.Entity<Attendance>()
            .HasIndex(a => a.StudentId);

        modelBuilder.Entity<Attendance>()
            .HasIndex(a => a.GroupId);
        
        modelBuilder.Entity<Comment>()
            .HasIndex(c => c.StudentId);
            
        modelBuilder.Entity<Comment>()
            .HasIndex(c => c.GroupId);
            
        modelBuilder.Entity<Comment>()
            .HasIndex(c => c.LessonId);

        modelBuilder.Entity<StudentPerformance>()
            .HasIndex(sp => sp.StudentId);
            
        modelBuilder.Entity<StudentPerformance>()
            .HasIndex(sp => sp.GroupId);
            
        modelBuilder.Entity<StudentStatistics>()
            .HasIndex(ss => ss.StudentId);
            
        modelBuilder.Entity<StudentStatistics>()
            .HasIndex(ss => ss.GroupId);
            
        modelBuilder.Entity<MonthlySummary>()
            .HasIndex(ms => ms.CenterId);
            
        modelBuilder.Entity<MonthlySummary>()
            .HasIndex(ms => new { ms.Month, ms.Year });
            
        modelBuilder.Entity<NotificationLog>()
            .HasIndex(nl => nl.StudentId);
            
        modelBuilder.Entity<NotificationLog>()
            .HasIndex(nl => nl.GroupId);
            
        modelBuilder.Entity<NotificationLog>()
            .HasIndex(nl => nl.CenterId);
            
        modelBuilder.Entity<NotificationLog>()
            .HasIndex(nl => nl.Type);
    }
}