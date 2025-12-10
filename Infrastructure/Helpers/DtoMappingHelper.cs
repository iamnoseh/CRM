using Domain.DTOs.User;
using Domain.DTOs.Student;
using Domain.DTOs.Lead;
using Domain.DTOs.Group;
using Domain.DTOs.Center;
using Domain.DTOs.Course;
using Domain.DTOs.Mentor;
using Domain.DTOs.Classroom;
using Domain.Entities;
using Domain.DTOs.Schedule;
using Domain.DTOs.Journal;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Infrastructure.Data;
using Infrastructure.Constants;
using Domain.DTOs.Payments;
using Microsoft.EntityFrameworkCore;
using Domain.DTOs.User.Employee;
using Domain.DTOs.Discounts;

namespace Infrastructure.Helpers;

public static class DtoMappingHelper
{
    #region User Mapping

    public static async Task<GetUserDto> MapToGetUserDtoAsync(User user, UserManager<User> userManager)
    {
        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault();

        return new GetUserDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            Address = user.Address,
            Gender = user.Gender,
            ActiveStatus = user.ActiveStatus,
            Age = user.Age,
            DateOfBirth = user.Birthday,
            Image = user.ProfileImagePath,
            Role = role,
            CenterId = user.CenterId
        };
    }

    public static GetUserDto MapToGetUserDtoSync(User user, string? role = null)
    {
        return new GetUserDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            Address = user.Address,
            Gender = user.Gender,
            ActiveStatus = user.ActiveStatus,
            Age = user.Age,
            DateOfBirth = user.Birthday,
            Image = user.ProfileImagePath,
            Role = role,
            CenterId = user.CenterId
        };
    }

    public static GetUserDetailsDto MapToGetUserDetailsDto(User user, int principalId, string? role = null)
    {
        return new GetUserDetailsDto
        {
            UserId = principalId,
            Username = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Gender = user.Gender,
            ActiveStatus = user.ActiveStatus,
            PaymentStatus = user.PaymentStatus,
            Age = user.Age,
            DateOfBirth = user.Birthday,
            Image = user.ProfileImagePath,
            DocumentPath = user.DocumentPath,
            CenterId = user.CenterId,
            CenterName = user.Center?.Name,
            Salary = user.Salary,
            Experience = user.Experience,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            EmailNotificationsEnabled = user.EmailNotificationsEnabled,
            TelegramNotificationsEnabled = user.TelegramNotificationsEnabled,
            TelegramChatId = user.TelegramChatId,
            Role = role
        };
    }

    #endregion

    #region Student Mapping

    public static GetStudentDto MapToGetStudentDto(Student student, string? userImagePath = null)
    {
        return new GetStudentDto
        {
            Id = student.Id,
            FullName = student.FullName,
            Email = student.Email,
            Address = student.Address,
            Phone = student.PhoneNumber,
            Birthday = student.Birthday,
            Age = student.Age,
            Gender = student.Gender,
            ActiveStatus = student.ActiveStatus,
            PaymentStatus = student.PaymentStatus,
            ImagePath = userImagePath ?? student.ProfileImage,
            Document = student.Document,
            UserId = student.UserId,
            CenterId = student.CenterId
        };
    }

    public static GetStudentForSelectDto MapToGetStudentForSelectDto(Student student)
    {
        return new GetStudentForSelectDto
        {
            Id = student.Id,
            FullName = student.FullName
        };
    }

    #endregion

    #region Group Mapping

    public static GetGroupDto MapToGetGroupDto(Group group)
    {
        return new GetGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            DurationMonth = group.DurationMonth,
            LessonInWeek = group.LessonInWeek,
            TotalWeeks = group.TotalWeeks,
            Started = group.Started,
            HasWeeklyExam = group.HasWeeklyExam,
            CurrentStudentsCount = group.StudentGroups.Count(sg => !sg.IsDeleted),
            Status = group.Status,
            StartDate = group.StartDate,
            EndDate = group.EndDate,
            Mentor = group.Mentor != null ? new GetSimpleDto
            {
                Id = group.Mentor.Id,
                FullName = group.Mentor.FullName
            } : null,
            DayOfWeek = 0,
            Course = group.Course != null ? new GetSimpleCourseDto
            {
                Id = group.Course.Id,
                CourseName = group.Course.CourseName
            } : null,
            ImagePath = group.PhotoPath,
            CurrentWeek = group.CurrentWeek,
            ClassroomId = group.ClassroomId,
            Classroom = group.Classroom != null ? new GetClassroomDto
            {
                Id = group.Classroom.Id,
                Name = group.Classroom.Name,
                Description = group.Classroom.Description,
                Capacity = group.Classroom.Capacity,
                IsActive = group.Classroom.IsActive,
                Center = (group.Classroom.Center != null ? new GetCenterSimpleDto
                {
                    Id = group.Classroom.Center.Id,
                    Name = group.Classroom.Center.Name
                } : null)!,
                CreatedAt = group.Classroom.CreatedAt,
                UpdatedAt = group.Classroom.UpdatedAt
            } : null,
            LessonDays = !string.IsNullOrEmpty(group.LessonDays) ? group.LessonDays : null,
            LessonStartTime = group.LessonStartTime,
            LessonEndTime = group.LessonEndTime
        };
    }

    public static GetSimpleGroupInfoDto MapToGetSimpleGroupInfoDto(Group group)
    {
        return new GetSimpleGroupInfoDto
        {
            Id = group.Id,
            Name = group.Name,
            ImagePath = group.PhotoPath
        };
    }

    #endregion

    #region Center Mapping

    public static GetCenterDto MapToGetCenterDto(Center c, DataContext context)
    {
        return new GetCenterDto
        {
            Id = c.Id,
            Name = c.Name,
            Address = c.Address,
            Description = c.Description,
            Image = c.Image,
            StudentCapacity = c.StudentCapacity,
            IsActive = c.IsActive,
            ContactEmail = c.Email,
            ContactPhone = c.ContactPhone,
            ManagerId = c.ManagerId,
            ManagerFullName = c.Manager != null ? c.Manager.FullName : null,
            TotalStudents = context.Students.Count(s => s.CenterId == c.Id && !s.IsDeleted),
            TotalMentors = context.Mentors.Count(m => m.CenterId == c.Id && !m.IsDeleted),
            TotalCourses = context.Courses.Count(co => co.CenterId == c.Id && !co.IsDeleted),
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
    }

    public static GetCenterSimpleDto MapToGetCenterSimpleDto(Center center)
    {
        return new GetCenterSimpleDto
        {
            Id = center.Id,
            Name = center.Name
        };
    }

    #endregion

    #region Course Mapping

    public static GetCourseDto MapToGetCourseDto(Course course)
    {
        return new GetCourseDto
        {
            Id = course.Id,
            CourseName = course.CourseName,
            Description = course.Description,
            Price = course.Price,
            DurationInMonth = course.DurationInMonth,
            Status = course.Status,
            ImagePath = course.ImagePath,
            CenterId = course.CenterId
        };
    }

    public static GetSimpleCourseDto MapToGetSimpleCourseDto(Course course)
    {
        return new GetSimpleCourseDto
        {
            Id = course.Id,
            CourseName = course.CourseName
        };
    }

    #endregion

    #region Mentor Mapping

    public static GetMentorDto MapToGetMentorDto(Mentor mentor, string? userImagePath = null)
    {
        return new GetMentorDto
        {
            Id = mentor.Id,
            FullName = mentor.FullName,
            Email = mentor.Email,
            Address = mentor.Address,
            Phone = mentor.PhoneNumber,
            Birthday = mentor.Birthday,
            Age = mentor.Age,
            Gender = mentor.Gender,
            ActiveStatus = mentor.ActiveStatus,
            ImagePath = userImagePath ?? mentor.ProfileImage,
            Document = mentor.Document,
            Experience = mentor.Experience,
            UserId = mentor.UserId,
            CenterId = mentor.CenterId
        };
    }

    #endregion

    #region Common Mappings

    public static GetSimpleDto MapToGetSimpleDto(int id, string fullName)
    {
        return new GetSimpleDto
        {
            Id = id,
            FullName = fullName
        };
    }

    #endregion
    #region Schedule Mapping

    public static GetScheduleDto MapToGetScheduleDto(Schedule schedule)
    {
        return new GetScheduleDto
        {
            Id = schedule.Id,
            ClassroomId = schedule.ClassroomId,
            Classroom = new GetClassroomDto
            {
                Id = schedule.Classroom!.Id,
                Name = schedule.Classroom.Name,
                Description = schedule.Classroom.Description,
                Capacity = schedule.Classroom.Capacity,
                IsActive = schedule.Classroom.IsActive,
                CenterId = schedule.Classroom.CenterId,
                Center = new GetCenterSimpleDto
                {
                    Id = schedule.Classroom!.Center!.Id,
                    Name = schedule.Classroom.Center.Name
                },
                CreatedAt = schedule.Classroom.CreatedAt,
                UpdatedAt = schedule.Classroom.UpdatedAt
            },
            GroupId = schedule.GroupId,
            Group = schedule.Group != null ? new GetGroupDto
            {
                Id = schedule.Group.Id,
                Name = schedule.Group.Name,
                Description = schedule.Group.Description
            } : null,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            DayOfWeek = schedule.DayOfWeek,
            StartDate = schedule.StartDate,
            EndDate = schedule.EndDate,
            IsRecurring = schedule.IsRecurring,
            Status = schedule.Status,
            Notes = schedule.Notes,
            CreatedAt = schedule.CreatedAt,
            UpdatedAt = schedule.UpdatedAt
        };
    }

    public static GetScheduleSimpleDto MapToGetScheduleSimpleDto(Schedule schedule)
    {
        return new GetScheduleSimpleDto
        {
            Id = schedule.Id,
            GroupName = schedule.Group?.Name ?? Messages.Common.Unknown,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            DayOfWeek = schedule.DayOfWeek,
            StartDate = schedule.StartDate,
            EndDate = schedule.EndDate,
            IsRecurring = schedule.IsRecurring,
            Status = schedule.Status
        };
    }

    #endregion

    #region Journal Mapping

    public static async Task<GetJournalDto> MapToJournalDtoAsync(Journal journal, DataContext context, bool includeDayNames = true)
    {
        var studentIds = journal.Entries.Select(e => e.StudentId).Distinct().ToList();
        var students = await context.Students
            .Where(s => studentIds.Contains(s.Id) && !s.IsDeleted)
            .Select(s => new { s.Id, s.FullName, IsActive = s.ActiveStatus == ActiveStatus.Active })
            .ToListAsync();

        var totalsByStudent = journal.Entries
            .Where(e => !e.IsDeleted)
            .GroupBy(e => e.StudentId)
            .ToDictionary(
                g => g.Key,
                g => g.Where(x => x.Grade.HasValue).Sum(x => x.Grade!.Value)
                     + g.Where(x => x.BonusPoints.HasValue).Sum(x => x.BonusPoints!.Value)
            );

        var progresses = students
            .Select(s => new { s, total = totalsByStudent.TryGetValue(s.Id, out var t) ? t : 0m })
            .OrderByDescending(x => x.total)
            .ThenByDescending(x => x.s.IsActive)
            .ThenBy(x => x.s.FullName)
            .Select(x => new StudentProgress
            {
                StudentId = x.s.Id,
                StudentName = x.s.FullName.Trim(),
                WeeklyTotalScores = (double)x.total,
                StudentEntries = journal.Entries
                    .Where(e => e.StudentId == x.s.Id)
                    .OrderBy(e => e.LessonNumber)
                    .ThenBy(e => e.DayOfWeek)
                    .Select(e => new GetJournalEntryDto
                    {
                        Id = e.Id,
                        DayOfWeek = e.DayOfWeek,
                        DayName = includeDayNames ? GetDayNameInTajik(e.DayOfWeek) : string.Empty,
                        DayShortName = includeDayNames ? GetDayShortNameInTajik(e.DayOfWeek) : string.Empty,
                        LessonNumber = e.LessonNumber,
                        LessonType = e.LessonType,
                        Grade = e.Grade ?? 0,
                        BonusPoints = e.BonusPoints ?? 0,
                        AttendanceStatus = e.AttendanceStatus,
                        Comment = e.Comment,
                        CommentCategory = e.CommentCategory ?? CommentCategory.General,
                        EntryDate = e.EntryDate,
                        StartTime = e.StartTime,
                        EndTime = e.EndTime
                    }).ToList()
            }).ToList();

        return new GetJournalDto
        {
            Id = journal.Id,
            GroupId = journal.GroupId,
            GroupName = journal.Group?.Name,
            WeekNumber = journal.WeekNumber,
            WeekStartDate = journal.WeekStartDate,
            WeekEndDate = journal.WeekEndDate,
            Progresses = progresses
        };
    }

    private static string GetDayNameInTajik(int crmDayOfWeek)
    {
        return crmDayOfWeek switch
        {
            1 => "Душанбе",
            2 => "Сешанбе",
            3 => "Чоршанбе",
            4 => "Панҷшанбе",
            5 => "Ҷумъа",
            6 => "Шанбе",
            7 => "Якшанбе",
            _ => Messages.Common.Unknown
        };
    }

    private static string GetDayShortNameInTajik(int crmDayOfWeek)
    {
        return crmDayOfWeek switch
        {
            1 => "Ду",
            2 => "Се",
            3 => "Чо",
            4 => "Па",
            5 => "Ҷу",
            6 => "Ша",
            7 => "Як",
            _ => "Н"
        };
    }

    #endregion

    #region Lead Mapping

    public static GetLeadDto MapToGetLeadDto(Lead lead)
    {
        return new GetLeadDto
        {
            Id = lead.Id,
            FullName = lead.FullName,
            PhoneNumber = lead.PhoneNumber,
            BirthDate = lead.BirthDate,
            Gender = lead.Gender,
            OccupationStatus = lead.OccupationStatus,
            RegisterForMonth = lead.RegisterForMonth,
            Course = lead.Course ?? string.Empty,
            LessonTime = lead.LessonTime,
            Notes = lead.Notes,
            UtmSource = lead.UtmSource,
            CenterId = lead.CenterId,
            CenterName = lead.Center != null ? lead.Center.Name : string.Empty,
            CreatedAt = lead.CreatedAt,
            UpdatedAt = lead.UpdatedAt
        };
    }

    #endregion

    #region Payment Mapping

    public static GetPaymentDto MapToGetPaymentDto(Payment payment)
    {
        return new GetPaymentDto
        {
            Id = payment.Id,
            StudentId = payment.StudentId,
            GroupId = payment.GroupId,
            ReceiptNumber = payment.ReceiptNumber,
            OriginalAmount = payment.OriginalAmount,
            DiscountAmount = payment.DiscountAmount,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            TransactionId = payment.TransactionId,
            Description = payment.Description,
            Status = payment.Status,
            PaymentDate = payment.PaymentDate,
            CenterId = payment.CenterId,
            Month = payment.Month,
            Year = payment.Year
        };
    }

    #endregion

    #region Classroom Mapping

    public static GetClassroomDto MapToGetClassroomDto(Classroom classroom)
    {
        return new GetClassroomDto
        {
            Id = classroom.Id,
            Name = classroom.Name,
            Description = classroom.Description,
            Capacity = classroom.Capacity,
            IsActive = classroom.IsActive,
            CenterId = classroom.CenterId,
            Center = classroom.Center != null ? new GetCenterSimpleDto
            {
                Id = classroom.Center.Id,
                Name = classroom.Center.Name
            } : null!,
            CreatedAt = classroom.CreatedAt,
            UpdatedAt = classroom.UpdatedAt
        };
    }

    public static GetSimpleClassroomDto MapToGetSimpleClassroomDto(Classroom classroom)
    {
        return new GetSimpleClassroomDto
        {
            Id = classroom.Id,
            Name = classroom.Name
        };
    }

    #endregion

    #region Employee Mapping

    public static GetEmployeeDto MapToGetEmployeeDto(User user, string? role = null)
    {
        return new GetEmployeeDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Address = user.Address,
            PhoneNumber = user.PhoneNumber,
            Role = role,
            Salary = user.Salary,
            Birthday = user.Birthday,
            Age = user.Age,
            Experience = user.Experience,
            Gender = user.Gender,
            ActiveStatus = user.ActiveStatus,
            PaymentStatus = user.PaymentStatus,
            ImagePath = user.ProfileImagePath,
            DocumentPath = user.DocumentPath,
            CenterId = user.CenterId
        };
    }

    public static ManagerSelectDto MapToManagerSelectDto(User user)
    {
        return new ManagerSelectDto
        {
            Id = user.Id,
            FullName = user.FullName
        };
    }

    #endregion
    #region Discount Mapping

    public static GetStudentGroupDiscountDto MapToGetStudentGroupDiscountDto(StudentGroupDiscount entity)
    {
        return new GetStudentGroupDiscountDto
        {
            Id = entity.Id,
            StudentId = entity.StudentId,
            GroupId = entity.GroupId,
            DiscountAmount = entity.DiscountAmount,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static DiscountPreviewDto MapToDiscountPreviewDto(decimal original, decimal discount, decimal net)
    {
        return new DiscountPreviewDto
        {
            OriginalAmount = original,
            DiscountAmount = discount,
            PayableAmount = net
        };
    }

    #endregion
}
