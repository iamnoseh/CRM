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
    public DbSet<StudentGroup> StudentGroups { get; set; }
    public DbSet<MentorGroup> MentorGroups { get; set; }
    public DbSet<Center> Centers { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Classroom> Classrooms { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all DateTime properties to use timestamp with time zone
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamp with time zone");
                }
            }
        }

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

        // Center vs Manager (User)
        modelBuilder.Entity<Center>()
            .HasOne(c => c.Manager)
            .WithOne(u => u.ManagedCenter)
            .HasForeignKey<Center>(c => c.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

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
            
       
        modelBuilder.Entity<Group>()
            .HasIndex(g => g.CourseId);

        modelBuilder.Entity<Group>()
            .HasIndex(g => g.MentorId);

        // Group lesson scheduling configuration
        modelBuilder.Entity<Group>()
            .Property(g => g.LessonDays)
            .HasMaxLength(50); // For comma-separated day numbers

        modelBuilder.Entity<Group>()
            .Property(g => g.LessonStartTime)
            .HasColumnType("time");

        modelBuilder.Entity<Group>()
            .Property(g => g.LessonEndTime)
            .HasColumnType("time");

        modelBuilder.Entity<Lesson>()
            .HasIndex(l => l.GroupId);

        // Classroom vs Center
        modelBuilder.Entity<Classroom>()
            .HasOne(c => c.Center)
            .WithMany(center => center.Classrooms)
            .HasForeignKey(c => c.CenterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Group vs Classroom
        modelBuilder.Entity<Group>()
            .HasOne(g => g.Classroom)
            .WithMany()
            .HasForeignKey(g => g.ClassroomId)
            .OnDelete(DeleteBehavior.SetNull);

        // Schedule vs Classroom
        modelBuilder.Entity<Schedule>()
            .HasOne(s => s.Classroom)
            .WithMany(c => c.Schedules)
            .HasForeignKey(s => s.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);

        // Schedule vs Group
        modelBuilder.Entity<Schedule>()
            .HasOne(s => s.Group)
            .WithMany(g => g.Schedules)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        // Lesson vs Classroom
        modelBuilder.Entity<Lesson>()
            .HasOne(l => l.Classroom)
            .WithMany(c => c.Lessons)
            .HasForeignKey(l => l.ClassroomId)
            .OnDelete(DeleteBehavior.SetNull);

        // Lesson vs Schedule
        modelBuilder.Entity<Lesson>()
            .HasOne(l => l.Schedule)
            .WithMany(s => s.Lessons)
            .HasForeignKey(l => l.ScheduleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for new entities
        modelBuilder.Entity<Classroom>()
            .HasIndex(c => c.CenterId);

        modelBuilder.Entity<Schedule>()
            .HasIndex(s => s.ClassroomId);

        modelBuilder.Entity<Schedule>()
            .HasIndex(s => s.GroupId);

        modelBuilder.Entity<Schedule>()
            .HasIndex(s => new { s.ClassroomId, s.DayOfWeek, s.StartTime, s.EndTime });

        modelBuilder.Entity<Lesson>()
            .HasIndex(l => l.ClassroomId);

        modelBuilder.Entity<Lesson>()
            .HasIndex(l => l.ScheduleId);

        modelBuilder.Entity<Lesson>()
            .HasIndex(l => new { l.StartTime, l.EndTime, l.ClassroomId });
    }
}