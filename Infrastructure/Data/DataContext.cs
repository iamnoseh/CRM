using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class DataContext(DbContextOptions<DataContext> options)
    : IdentityDbContext<User, IdentityRole<int>, int>(options)
{
    public DbSet<Lead> Leads { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Mentor> Mentors { get; set; }
    public DbSet<StudentGroup> StudentGroups { get; set; }
    public DbSet<MentorGroup> MentorGroups { get; set; }
    public DbSet<Center> Centers { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<StudentGroupDiscount> StudentGroupDiscounts { get; set; }
    public DbSet<Classroom> Classrooms { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<Journal> Journals { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<MonthlyFinancialSummary> MonthlyFinancialSummaries { get; set; }
    public DbSet<StudentAccount> StudentAccounts { get; set; }
    public DbSet<AccountLog> AccountLogs { get; set; }
    public DbSet<PayrollContract> PayrollContracts { get; set; }
    public DbSet<WorkLog> WorkLogs { get; set; }
    public DbSet<PayrollRecord> PayrollRecords { get; set; }
    public DbSet<Advance> Advances { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
        
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<IdentityRole<int>>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        
        
        // Center vs Manager (User)
        modelBuilder.Entity<Center>()
            .HasOne(c => c.Manager)
            .WithOne(u => u.ManagedCenter)
            .HasForeignKey<Center>(c => c.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Center vs Users
        modelBuilder.Entity<User>()
            .HasOne(u => u.Center)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.CenterId)
            .OnDelete(DeleteBehavior.SetNull);
        
        
        // Course vs Center
        modelBuilder.Entity<Course>()
            .HasOne(c => c.Center)
            .WithMany(center => center.Courses)
            .HasForeignKey(c => c.CenterId)
            .OnDelete(DeleteBehavior.Cascade);
        
        
        // Group vs Course
        modelBuilder.Entity<Group>()
            .HasOne(g => g.Course)
            .WithMany(c => c.Groups)
            .HasForeignKey(g => g.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Group vs Mentor
        modelBuilder.Entity<Group>()
            .HasOne(g => g.Mentor)
            .WithMany(m => m.Groups)
            .HasForeignKey(g => g.MentorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Group vs Classroom
        modelBuilder.Entity<Group>()
            .HasOne(g => g.Classroom)
            .WithMany(c => c.Groups)
            .HasForeignKey(g => g.ClassroomId)
            .OnDelete(DeleteBehavior.SetNull);
        
        
        // Student vs Center
        modelBuilder.Entity<Student>()
            .HasOne(s => s.Center)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.CenterId)
            .OnDelete(DeleteBehavior.SetNull);

        // Student vs User (One-to-One)
        modelBuilder.Entity<Student>()
            .HasOne(s => s.User)
            .WithOne(u => u.StudentProfile)
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        
        // Mentor vs Center
        modelBuilder.Entity<Mentor>()
            .HasOne(m => m.Center)
            .WithMany(c => c.Mentors)
            .HasForeignKey(m => m.CenterId)
            .OnDelete(DeleteBehavior.SetNull);

        // Mentor vs User (One-to-One)
        modelBuilder.Entity<Mentor>()
            .HasOne(m => m.User)
            .WithOne(u => u.MentorProfile)
            .HasForeignKey<Mentor>(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);


        // StudentGroup (Student ↔ Group Many-to-Many)
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

        // MentorGroup (Mentor ↔ Group Many-to-Many)
        modelBuilder.Entity<MentorGroup>()
            .HasKey(mg => new { mg.MentorId, mg.GroupId });

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

        
        // Payment vs Student
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Student)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Payment vs Group
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Group)
            .WithMany()
            .HasForeignKey(p => p.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        // Payment vs Center
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Center)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.CenterId)
            .OnDelete(DeleteBehavior.SetNull);

        // StudentAccount vs Student
        modelBuilder.Entity<StudentAccount>()
            .HasOne(sa => sa.Student)
            .WithMany()
            .HasForeignKey(sa => sa.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // AccountLog vs Account
        modelBuilder.Entity<AccountLog>()
            .HasOne(al => al.Account)
            .WithMany()
            .HasForeignKey(al => al.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Expense vs Center
        modelBuilder.Entity<Expense>()
            .HasOne(e => e.Center)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.CenterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Expense vs Mentor
        modelBuilder.Entity<Expense>()
            .HasOne(e => e.Mentor)
            .WithMany()
            .HasForeignKey(e => e.MentorId)
            .OnDelete(DeleteBehavior.SetNull);

        // MonthlyFinancialSummary vs Center
        modelBuilder.Entity<MonthlyFinancialSummary>()
            .HasOne(m => m.Center)
            .WithMany(c => c.MonthlySummaries)
            .HasForeignKey(m => m.CenterId)
            .OnDelete(DeleteBehavior.Cascade);

        
        // Classroom vs Center
        modelBuilder.Entity<Classroom>()
            .HasOne(c => c.Center)
            .WithMany(center => center.Classrooms)
            .HasForeignKey(c => c.CenterId)
            .OnDelete(DeleteBehavior.Cascade);

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
        
        
        // Journal vs Group
        modelBuilder.Entity<Journal>()
            .HasOne(j => j.Group)
            .WithMany(g => g.Journals)
            .HasForeignKey(j => j.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // JournalEntry vs Journal
        modelBuilder.Entity<JournalEntry>()
            .HasOne(je => je.Journal)
            .WithMany(j => j.Entries)
            .HasForeignKey(je => je.JournalId)
            .OnDelete(DeleteBehavior.Cascade);

        // JournalEntry vs Student
        modelBuilder.Entity<JournalEntry>()
            .HasOne(je => je.Student)
            .WithMany(s => s.JournalEntries)
            .HasForeignKey(je => je.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        
        
        // Group properties
        modelBuilder.Entity<Group>()
            .Property(g => g.LessonDays)
            .HasMaxLength(50);

        modelBuilder.Entity<Group>()
            .Property(g => g.LessonStartTime)
            .HasColumnType("time");

        modelBuilder.Entity<Group>()
            .Property(g => g.LessonEndTime)
            .HasColumnType("time");

        // JournalEntry time properties
        modelBuilder.Entity<JournalEntry>()
            .Property(je => je.StartTime)
            .HasColumnType("time");

        modelBuilder.Entity<JournalEntry>()
            .Property(je => je.EndTime)
            .HasColumnType("time");

        // Payment decimal precision
        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payment>()
            .Property(p => p.OriginalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payment>()
            .Property(p => p.DiscountAmount)
            .HasPrecision(18, 2);

        // StudentGroupDiscount relations and precision
        modelBuilder.Entity<StudentGroupDiscount>()
            .HasOne(sgd => sgd.Student)
            .WithMany()
            .HasForeignKey(sgd => sgd.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentGroupDiscount>()
            .HasOne(sgd => sgd.Group)
            .WithMany()
            .HasForeignKey(sgd => sgd.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentGroupDiscount>()
            .Property(sgd => sgd.DiscountAmount)
            .HasPrecision(18, 2);

        // Student TotalPaid precision
        modelBuilder.Entity<Student>()
            .Property(s => s.TotalPaid)
            .HasPrecision(18, 2);

        // Mentor Salary precision
        modelBuilder.Entity<Mentor>()
            .Property(m => m.Salary)
            .HasPrecision(18, 2);

        // Course Price precision
        modelBuilder.Entity<Course>()
            .Property(c => c.Price)
            .HasPrecision(18, 2);

        // Expense Amount precision
        modelBuilder.Entity<Expense>()
            .Property(e => e.Amount)
            .HasPrecision(18, 2);

        // StudentAccount precision
        modelBuilder.Entity<StudentAccount>()
            .Property(a => a.Balance)
            .HasPrecision(18, 2);

        // MonthlyFinancialSummary precision
        modelBuilder.Entity<MonthlyFinancialSummary>()
            .Property(m => m.TotalIncome)
            .HasPrecision(18, 2);

        modelBuilder.Entity<MonthlyFinancialSummary>()
            .Property(m => m.TotalExpense)
            .HasPrecision(18, 2);

        modelBuilder.Entity<MonthlyFinancialSummary>()
            .Property(m => m.NetProfit)
            .HasPrecision(18, 2);

        // Center income precision
        modelBuilder.Entity<Center>()
            .Property(c => c.MonthlyIncome)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Center>()
            .Property(c => c.YearlyIncome)
            .HasPrecision(18, 2);


        // User indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.CenterId);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email);

        // Student indexes
        modelBuilder.Entity<Student>()
            .HasIndex(s => s.CenterId);

        modelBuilder.Entity<Student>()
            .HasIndex(s => s.UserId)
            .IsUnique();
        

        // Mentor indexes
        modelBuilder.Entity<Mentor>()
            .HasIndex(m => m.CenterId);

        modelBuilder.Entity<Mentor>()
            .HasIndex(m => m.UserId)
            .IsUnique();
        

        // Course indexes
        modelBuilder.Entity<Course>()
            .HasIndex(c => c.CenterId);

        // Group indexes
        modelBuilder.Entity<Group>()
            .HasIndex(g => g.CourseId);

        modelBuilder.Entity<Group>()
            .HasIndex(g => g.MentorId);

        modelBuilder.Entity<Group>()
            .HasIndex(g => g.ClassroomId);

        // StudentGroup indexes
        modelBuilder.Entity<StudentGroup>()
            .HasIndex(sg => sg.StudentId);

        modelBuilder.Entity<StudentGroup>()
            .HasIndex(sg => sg.GroupId);

        // MentorGroup indexes
        modelBuilder.Entity<MentorGroup>()
            .HasIndex(mg => mg.MentorId);

        modelBuilder.Entity<MentorGroup>()
            .HasIndex(mg => mg.GroupId);

        // Payment indexes
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.StudentId);

        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.GroupId);

        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.CenterId);

        modelBuilder.Entity<Payment>()
            .HasIndex(p => new { p.Month, p.Year });

        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.PaymentDate);

        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.ReceiptNumber);

        // StudentAccount indexes
        modelBuilder.Entity<StudentAccount>()
            .HasIndex(a => a.StudentId);

        modelBuilder.Entity<StudentAccount>()
            .HasIndex(a => a.AccountCode)
            .IsUnique();

        // AccountLog indexes
        modelBuilder.Entity<AccountLog>()
            .HasIndex(l => l.AccountId);

        // StudentGroupDiscount indexes and unique constraint (one active per Student-Group)
        modelBuilder.Entity<StudentGroupDiscount>()
            .HasIndex(sgd => sgd.StudentId);

        modelBuilder.Entity<StudentGroupDiscount>()
            .HasIndex(sgd => sgd.GroupId);

        modelBuilder.Entity<StudentGroupDiscount>()
            .HasIndex(sgd => new { sgd.StudentId, sgd.GroupId, sgd.IsDeleted })
            .HasDatabaseName("IX_StudentGroupDiscount_Student_Group_IsDeleted");

        // Expense indexes
        modelBuilder.Entity<Expense>()
            .HasIndex(e => e.CenterId);

        modelBuilder.Entity<Expense>()
            .HasIndex(e => e.MentorId);

        modelBuilder.Entity<Expense>()
            .HasIndex(e => new { e.Month, e.Year });

        modelBuilder.Entity<Expense>()
            .HasIndex(e => e.ExpenseDate);

        modelBuilder.Entity<Expense>()
            .HasIndex(e => e.Category);

        // MonthlyFinancialSummary indexes
        modelBuilder.Entity<MonthlyFinancialSummary>()
            .HasIndex(m => new { m.CenterId, m.Year, m.Month })
            .IsUnique()
            .HasDatabaseName("IX_MonthlyFinancialSummary_Center_Year_Month");

        // Classroom indexes
        modelBuilder.Entity<Classroom>()
            .HasIndex(c => c.CenterId);

        // Schedule indexes
        modelBuilder.Entity<Schedule>()
            .HasIndex(s => s.ClassroomId);

        modelBuilder.Entity<Schedule>()
            .HasIndex(s => s.GroupId);

        modelBuilder.Entity<Schedule>()
            .HasIndex(s => new { s.ClassroomId, s.DayOfWeek, s.StartTime, s.EndTime })
            .HasDatabaseName("IX_Schedule_Classroom_Time");

        // Journal indexes
        modelBuilder.Entity<Journal>()
            .HasIndex(j => j.GroupId);

        modelBuilder.Entity<Journal>()
            .HasIndex(j => j.WeekNumber);

        modelBuilder.Entity<Journal>()
            .HasIndex(j => new { j.GroupId, j.WeekNumber })
            .IsUnique()
            .HasDatabaseName("IX_Journal_Group_Week");

        // JournalEntry indexes
        modelBuilder.Entity<JournalEntry>()
            .HasIndex(je => je.JournalId);

        modelBuilder.Entity<JournalEntry>()
            .HasIndex(je => je.StudentId);

        modelBuilder.Entity<JournalEntry>()
            .HasIndex(je => new { je.JournalId, je.DayOfWeek, je.LessonNumber })
            .HasDatabaseName("IX_JournalEntry_Journal_Day_Lesson");

        modelBuilder.Entity<JournalEntry>()
            .HasIndex(je => new { je.StudentId, je.JournalId })
            .HasDatabaseName("IX_JournalEntry_Student_Journal");

        modelBuilder.Entity<JournalEntry>()
            .HasIndex(je => je.CommentCategory)
            .HasDatabaseName("IX_JournalEntry_CommentCategory");
        
        
        // JournalEntry unique constraint (1 entry per student per lesson)
        modelBuilder.Entity<JournalEntry>()
            .HasIndex(je => new { je.JournalId, je.StudentId, je.DayOfWeek, je.LessonNumber })
            .IsUnique()
            .HasDatabaseName("IX_JournalEntry_Unique");

        
        // Convert enums to strings for PostgreSQL
        modelBuilder.Entity<User>()
            .Property(u => u.Gender)
            .HasConversion<string>();

        modelBuilder.Entity<User>()
            .Property(u => u.ActiveStatus)
            .HasConversion<string>();

        modelBuilder.Entity<User>()
            .Property(u => u.PaymentStatus)
            .HasConversion<string>();

        modelBuilder.Entity<Student>()
            .Property(s => s.Gender)
            .HasConversion<string>();

        modelBuilder.Entity<Student>()
            .Property(s => s.ActiveStatus)
            .HasConversion<string>();

        modelBuilder.Entity<Student>()
            .Property(s => s.PaymentStatus)
            .HasConversion<string>();

        modelBuilder.Entity<Mentor>()
            .Property(m => m.Gender)
            .HasConversion<string>();

        modelBuilder.Entity<Mentor>()
            .Property(m => m.ActiveStatus)
            .HasConversion<string>();

        modelBuilder.Entity<Mentor>()
            .Property(m => m.PaymentStatus)
            .HasConversion<string>();

        modelBuilder.Entity<Course>()
            .Property(c => c.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Group>()
            .Property(g => g.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Schedule>()
            .Property(s => s.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Payment>()
            .Property(p => p.PaymentMethod)
            .HasConversion<string>();

        modelBuilder.Entity<Payment>()
            .Property(p => p.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Expense>()
            .Property(e => e.PaymentMethod)
            .HasConversion<string>();

        modelBuilder.Entity<Expense>()
            .Property(e => e.Category)
            .HasConversion<string>();

        modelBuilder.Entity<JournalEntry>()
            .Property(je => je.LessonType)
            .HasConversion<string>();

        modelBuilder.Entity<JournalEntry>()
            .Property(je => je.AttendanceStatus)
            .HasConversion<string>();

        modelBuilder.Entity<JournalEntry>()
            .Property(je => je.CommentCategory)
            .HasConversion<string>();

        // PayrollContract configuration
        modelBuilder.Entity<PayrollContract>(entity =>
        {
            entity.HasOne(p => p.Mentor)
                .WithMany()
                .HasForeignKey(p => p.MentorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.EmployeeUser)
                .WithMany()
                .HasForeignKey(p => p.EmployeeUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Center)
                .WithMany()
                .HasForeignKey(p => p.CenterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(p => p.FixedAmount).HasPrecision(18, 2);
            entity.Property(p => p.HourlyRate).HasPrecision(18, 2);
            entity.Property(p => p.StudentPercentage).HasPrecision(5, 2);

            entity.Property(p => p.SalaryType).HasConversion<string>();

            entity.HasIndex(p => p.MentorId);
            entity.HasIndex(p => p.EmployeeUserId);
            entity.HasIndex(p => p.CenterId);
        });

        // WorkLog configuration
        modelBuilder.Entity<WorkLog>(entity =>
        {
            entity.HasOne(w => w.Mentor)
                .WithMany()
                .HasForeignKey(w => w.MentorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(w => w.EmployeeUser)
                .WithMany()
                .HasForeignKey(w => w.EmployeeUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(w => w.Center)
                .WithMany()
                .HasForeignKey(w => w.CenterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(w => w.Group)
                .WithMany()
                .HasForeignKey(w => w.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(w => w.Hours).HasPrecision(5, 2);

            entity.HasIndex(w => w.MentorId);
            entity.HasIndex(w => w.EmployeeUserId);
            entity.HasIndex(w => new { w.Month, w.Year });
        });

        // PayrollRecord configuration
        modelBuilder.Entity<PayrollRecord>(entity =>
        {
            entity.HasOne(p => p.Mentor)
                .WithMany()
                .HasForeignKey(p => p.MentorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.EmployeeUser)
                .WithMany()
                .HasForeignKey(p => p.EmployeeUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Center)
                .WithMany()
                .HasForeignKey(p => p.CenterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(p => p.FixedAmount).HasPrecision(18, 2);
            entity.Property(p => p.HourlyAmount).HasPrecision(18, 2);
            entity.Property(p => p.TotalHours).HasPrecision(5, 2);
            entity.Property(p => p.PercentageAmount).HasPrecision(18, 2);
            entity.Property(p => p.TotalStudentPayments).HasPrecision(18, 2);
            entity.Property(p => p.PercentageRate).HasPrecision(5, 2);
            entity.Property(p => p.BonusAmount).HasPrecision(18, 2);
            entity.Property(p => p.FineAmount).HasPrecision(18, 2);
            entity.Property(p => p.AdvanceDeduction).HasPrecision(18, 2);
            entity.Property(p => p.GrossAmount).HasPrecision(18, 2);
            entity.Property(p => p.NetAmount).HasPrecision(18, 2);

            entity.Property(p => p.Status).HasConversion<string>();
            entity.Property(p => p.PaymentMethod).HasConversion<string>();

            entity.HasIndex(p => p.MentorId);
            entity.HasIndex(p => p.EmployeeUserId);
            entity.HasIndex(p => new { p.Month, p.Year });
            entity.HasIndex(p => p.Status);
        });

        // Advance configuration
        modelBuilder.Entity<Advance>(entity =>
        {
            entity.HasOne(a => a.Mentor)
                .WithMany()
                .HasForeignKey(a => a.MentorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.EmployeeUser)
                .WithMany()
                .HasForeignKey(a => a.EmployeeUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Center)
                .WithMany()
                .HasForeignKey(a => a.CenterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.PayrollRecord)
                .WithMany()
                .HasForeignKey(a => a.PayrollRecordId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(a => a.Amount).HasPrecision(18, 2);

            entity.Property(a => a.Status).HasConversion<string>();

            entity.HasIndex(a => a.MentorId);
            entity.HasIndex(a => a.EmployeeUserId);
            entity.HasIndex(a => new { a.TargetMonth, a.TargetYear });
            entity.HasIndex(a => a.Status);
        });
    }
}