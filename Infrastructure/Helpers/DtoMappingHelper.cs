using Domain.DTOs.User;
using Domain.DTOs.Student;
using Domain.DTOs.Group;
using Domain.DTOs.Center;
using Domain.DTOs.Course;
using Domain.DTOs.Mentor;
using Domain.DTOs.Classroom;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

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
            CurrentStudentsCount = group.StudentGroups?.Count(sg => !sg.IsDeleted) ?? 0,
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

    public static GetCenterDto MapToGetCenterDto(Center center)
    {
        return new GetCenterDto
        {
            Id = center.Id,
            Name = center.Name,
            Address = center.Address,
            Description = center.Description,
            Image = center.Image,
            MonthlyIncome = 0,
            YearlyIncome = 0,
            StudentCapacity = center.StudentCapacity,
            IsActive = center.IsActive,
            ContactEmail = center.Email,
            ContactPhone = center.ContactPhone,
            ManagerId = center.ManagerId,
            CreatedAt = center.CreatedAt,
            UpdatedAt = center.UpdatedAt
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
            PaymentStatus = mentor.PaymentStatus,
            ImagePath = userImagePath ?? mentor.ProfileImage,
            Document = mentor.Document,
            Salary = mentor.Salary,
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
}
