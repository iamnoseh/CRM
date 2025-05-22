using System.Net;
using Domain.DTOs.Exam;
using Domain.DTOs.Grade;
using Domain.DTOs.Student;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Infrastructure.Services.EmailService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class StudentService(
    DataContext context,
    IHttpContextAccessor httpContextAccessor,
    UserManager<User> userManager,
    string uploadPath,
    IEmailService emailService) : IStudentService
{
    public async Task<Response<string>> CreateStudentAsync(CreateStudentDto createStudentDto)
    {
        try
        {
            // Загрузка изображения профиля
            string profileImagePath = string.Empty;
            if (createStudentDto.ProfilePhoto != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(
                    createStudentDto.ProfilePhoto, uploadPath, "student", "profile");
                if (imageResult.StatusCode != 200)
                    return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);
                profileImagePath = imageResult.Data;
            }

            // Загрузка документа
            string documentPath = string.Empty;
            if (createStudentDto.DocumentFile != null)
            {
                var docResult = await FileUploadHelper.UploadFileAsync(
                    createStudentDto.DocumentFile, uploadPath, "student", "document");
                if (docResult.StatusCode != 200)
                    return new Response<string>((HttpStatusCode)docResult.StatusCode, docResult.Message);
                documentPath = docResult.Data;
            }

            // Создание пользователя
            var userResult = await UserManagementHelper.CreateUserAsync(
                createStudentDto,
                userManager,
                Roles.Student,
                dto => dto.PhoneNumber,
                dto => dto.Email,
                dto => dto.FullName,
                dto => dto.Birthday,
                dto => dto.Gender,
                dto => dto.Address,
                dto => dto.CenterId,
                _ => profileImagePath);
            if (userResult.StatusCode != 200)
                return new Response<string>((HttpStatusCode)userResult.StatusCode, userResult.Message);

            var (user, password, username) = userResult.Data;

            // Отправка email
            if (!string.IsNullOrEmpty(createStudentDto.Email))
            {
                await EmailHelper.SendLoginDetailsEmailAsync(
                    emailService,
                    createStudentDto.Email,
                    username,
                    password,
                    "Student",
                    "#5E60CE",
                    "#4EA8DE");
            }

            // Создание студента
            var student = new Student
            {
                FullName = createStudentDto.FullName,
                Email = createStudentDto.Email,
                Address = createStudentDto.Address,
                PhoneNumber = createStudentDto.PhoneNumber,
                Birthday = createStudentDto.Birthday,
                Age = DateUtils.CalculateAge(createStudentDto.Birthday),
                Gender = createStudentDto.Gender,
                CenterId = createStudentDto.CenterId,
                UserId = user.Id,
                ProfileImage = profileImagePath,
                Document = documentPath,
                ActiveStatus = Domain.Enums.ActiveStatus.Active,
                PaymentStatus = Domain.Enums.PaymentStatus.Completed
            };

            await context.Students.AddAsync(student);
            var res = await context.SaveChangesAsync();

            return res > 0
                ? new Response<string>(HttpStatusCode.Created, "Student Created Successfully")
                : new Response<string>(HttpStatusCode.BadRequest, "Student Creation Failed");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<string>> UpdateStudentAsync(int id, UpdateStudentDto updateStudentDto)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, "Student not found");

        student.FullName = updateStudentDto.FullName;
        student.Email = updateStudentDto.Email;
        student.Address = updateStudentDto.Address;
        student.PhoneNumber = updateStudentDto.PhoneNumber;
        student.Birthday = updateStudentDto.Birthday;
        student.Age = DateUtils.CalculateAge(updateStudentDto.Birthday);
        student.Gender = updateStudentDto.Gender;
        student.ActiveStatus = updateStudentDto.ActiveStatus;
        student.PaymentStatus = updateStudentDto.PaymentStatus;
        student.UpdatedAt = DateTime.UtcNow;

        if (student.UserId != null)
        {
            var user = await userManager.FindByIdAsync(student.UserId.ToString());
            if (user != null)
            {
                var updateResult = await UserManagementHelper.UpdateUserAsync(
                    user,
                    updateStudentDto,
                    userManager,
                    dto => dto.Email,
                    dto => dto.FullName,
                    dto => dto.PhoneNumber,
                    dto => dto.Birthday,
                    dto => dto.Gender,
                    dto => dto.Address,
                    dto => dto.ActiveStatus,
                    dto => student.CenterId,
                    dto => dto.PaymentStatus);
                if (updateResult.StatusCode != 200)
                    return updateResult;
            }
        }

        context.Students.Update(student);
        var res = await context.SaveChangesAsync();

        return res > 0
            ? new Response<string>(HttpStatusCode.OK, "Student Updated Successfully")
            : new Response<string>(HttpStatusCode.BadRequest, "Student Update Failed");
    }

    public async Task<Response<string>> DeleteStudentAsync(int id)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, "Student not found");

        student.IsDeleted = true;
        student.UpdatedAt = DateTime.UtcNow;

        if (student.UserId != null)
        {
            var user = await userManager.FindByIdAsync(student.UserId.ToString());
            if (user != null)
            {
                user.IsDeleted = true;
                await userManager.UpdateAsync(user);
            }
        }

        context.Students.Update(student);
        var res = await context.SaveChangesAsync();

        return res > 0
            ? new Response<string>(HttpStatusCode.OK, "Student Deleted Successfully")
            : new Response<string>(HttpStatusCode.BadRequest, "Student Deletion Failed");
    }

    public async Task<Response<List<GetStudentDto>>> GetStudents()
    {
        var studentsQuery = context.Students
            .Where(s => !s.IsDeleted)
            .Select(s => new GetStudentDto
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                Address = s.Address,
                Phone = s.PhoneNumber,
                Birthday = s.Birthday,
                Age = s.Age,
                Gender = s.Gender,
                ActiveStatus = s.ActiveStatus,
                PaymentStatus = s.PaymentStatus,
                UserId = s.UserId,
                ImagePath = s.ProfileImage,
                Document = s.Document
            });

        var students = await studentsQuery.ToListAsync();

        foreach (var student in students)
        {
            if (student.UserId > 0)
            {
                var user = await userManager.FindByIdAsync(student.UserId.ToString());
                if (user != null)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    student.Role = roles.FirstOrDefault() ?? "Student";
                }
            }
        }

        return students.Any()
            ? new Response<List<GetStudentDto>>(students)
            : new Response<List<GetStudentDto>>(HttpStatusCode.NotFound, "No students found");
    }

    public async Task<Response<GetStudentDto>> GetStudentByIdAsync(int id)
    {
        var student = await context.Students
            .Where(s => s.Id == id && !s.IsDeleted)
            .Select(s => new GetStudentDto
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                Address = s.Address,
                Phone = s.PhoneNumber,
                Birthday = s.Birthday,
                Age = s.Age,
                Gender = s.Gender,
                ActiveStatus = s.ActiveStatus,
                PaymentStatus = s.PaymentStatus,
                ImagePath = s.ProfileImage,
                Document = s.Document
            })
            .FirstOrDefaultAsync();

        return student != null
            ? new Response<GetStudentDto>(student)
            : new Response<GetStudentDto>(HttpStatusCode.NotFound, "Student not found");
    }

    public async Task<Response<string>> UpdateStudentDocumentAsync(int studentId, IFormFile? documentFile)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, "Student not found");

        var docResult = await FileUploadHelper.UploadFileAsync(
            documentFile, uploadPath, "student", "document", true, student.Document);
        if (docResult.StatusCode != 200)
            return new Response<string>((HttpStatusCode)docResult.StatusCode, docResult.Message);

        student.Document = docResult.Data;
        student.UpdatedAt = DateTime.UtcNow;
        context.Students.Update(student);
        var res = await context.SaveChangesAsync();

        return res > 0
            ? new Response<string>(HttpStatusCode.OK, "Student document updated successfully")
            : new Response<string>(HttpStatusCode.BadRequest, "Failed to update student document");
    }

    public async Task<PaginationResponse<List<GetStudentDto>>> GetStudentsPagination(StudentFilter filter)
    {
        var studentsQuery = context.Students.Where(s => !s.IsDeleted).AsQueryable();

        if (!string.IsNullOrEmpty(filter.FullName))
            studentsQuery = studentsQuery.Where(s => s.FullName.Contains(filter.FullName));

        if (!string.IsNullOrEmpty(filter.Email))
            studentsQuery = studentsQuery.Where(s => s.Email.Contains(filter.Email));

        if (!string.IsNullOrEmpty(filter.PhoneNumber))
            studentsQuery = studentsQuery.Where(s => s.PhoneNumber.Contains(filter.PhoneNumber));

        if (filter.CenterId.HasValue)
            studentsQuery = studentsQuery.Where(s => s.CenterId == filter.CenterId.Value);

        if (filter.Active.HasValue)
            studentsQuery = studentsQuery.Where(s => s.ActiveStatus == filter.Active.Value);

        if (filter.PaymentStatus.HasValue)
            studentsQuery = studentsQuery.Where(s => s.PaymentStatus == filter.PaymentStatus.Value);

        var totalRecords = await studentsQuery.CountAsync();
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var students = await studentsQuery
            .OrderBy(s => s.Id)
            .Skip(skip)
            .Take(filter.PageSize)
            .Select(s => new GetStudentDto
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                Address = s.Address,
                Phone = s.PhoneNumber,
                Birthday = s.Birthday,
                Age = s.Age,
                Gender = s.Gender,
                ActiveStatus = s.ActiveStatus,
                PaymentStatus = s.PaymentStatus,
                ImagePath = s.ProfileImage
            })
            .ToListAsync();

        return new PaginationResponse<List<GetStudentDto>>(
            students,
            totalRecords,
            filter.PageNumber,
            filter.PageSize);
    }

    public async Task<Response<byte[]>> GetStudentDocument(int studentId)
    {
        var student = await context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);

        if (student == null)
            return new Response<byte[]>(HttpStatusCode.NotFound, "Student not found");

        return await FileUploadHelper.GetFileAsync(student.Document, uploadPath);
    }

    public async Task<Response<string>> UpdateUserProfileImageAsync(int studentId, IFormFile? profileImage)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, "Student not found");

        var imageResult = await FileUploadHelper.UploadFileAsync(
            profileImage, uploadPath, "student", "profile", true, student.ProfileImage);
        if (imageResult.StatusCode != 200)
            return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);

        student.ProfileImage = imageResult.Data;
        student.UpdatedAt = DateTime.UtcNow;

        if (student.UserId != null)
        {
            var user = await userManager.FindByIdAsync(student.UserId.ToString());
            if (user != null)
            {
                user.ProfileImagePath = student.ProfileImage;
                await userManager.UpdateAsync(user);
            }
        }

        context.Students.Update(student);
        var res = await context.SaveChangesAsync();

        return res > 0
            ? new Response<string>(HttpStatusCode.OK, "Profile image updated successfully")
            : new Response<string>(HttpStatusCode.BadRequest, "Failed to update profile image");
    }

    public async Task<Response<GetStudentDetailedDto>> GetStudentDetailedAsync(int id)
    {
        try
        {
            var student = await context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (student == null)
                return new Response<GetStudentDetailedDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Студент не найден"
                };

            var studentGroups = await context.StudentGroups
                .Include(sg => sg.Group)
                .Where(sg => sg.StudentId == id && (bool)sg.IsActive && !sg.IsDeleted)
                .ToListAsync();

            var recentGrades = await context.Grades
                .Include(g => g.Lesson)
                .ThenInclude(l => l.Group)
                .Where(g => g.StudentId == id && !g.IsDeleted)
                .OrderByDescending(g => g.CreatedAt)
                .Take(5)
                .Select(g => new GetGradeDto
                {
                    Id = g.Id,
                    Value = g.Value,
                    Comment = g.Comment,
                    BonusPoints = g.BonusPoints,
                    LessonId = g.LessonId,
                    CreatedAt = g.Lesson.StartTime,
                    GroupId = g.GroupId,
                    StudentId = g.StudentId
                })
                .ToListAsync();

            var recentExams = await context.Grades
                .Include(eg => eg.Exam)
                .ThenInclude(e => e.Group)
                .Where(eg => eg.StudentId == id && !eg.IsDeleted)
                .OrderByDescending(eg => eg.CreatedAt)
                .Take(1)
                .ToListAsync();

            var examDtos = recentExams.Select(eg => new GetExamDto
            {
                Id = eg.ExamId,
                GroupId = eg.Exam.GroupId,
                WeekIndex = eg.Exam.WeekIndex,
                ExamDate = eg.Exam.ExamDate,
            }).ToList();

            double averageGrade = 0;
            var allGrades = await context.Grades
                .Where(g => g.StudentId == id && g.Value.HasValue && !g.IsDeleted)
                .Select(g => g.Value.Value)
                .ToListAsync();

            if (allGrades.Any())
            {
                averageGrade = Math.Round(allGrades.Average(), 2);
            }

            var groupInfos = new List<GetStudentDetailedDto.GroupInfo>();
            foreach (var group in studentGroups)
            {
                var groupGrades = await context.Grades
                    .Where(g => g.StudentId == id &&
                               g.GroupId == group.GroupId &&
                               g.Value.HasValue &&
                               !g.IsDeleted)
                    .Select(g => g.Value.Value)
                    .ToListAsync();

                double groupAverage = 0;
                if (groupGrades.Any())
                {
                    groupAverage = Math.Round(groupGrades.Average(), 2);
                }

                groupInfos.Add(new GetStudentDetailedDto.GroupInfo
                {
                    GroupId = group.GroupId,
                    GroupName = group.Group.Name,
                    GroupAverageGrade = groupAverage
                });
            }

            var paymentStatus = Domain.Enums.PaymentStatus.Paid;
            var latestPayment = await context.Payments
                .Where(p => p.StudentId == id && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestPayment != null)
            {
                paymentStatus = latestPayment.Status;
            }

            var studentDetailed = new GetStudentDetailedDto
            {
                Id = student.Id,
                FullName = student.User.FullName,
                Email = student.User.Email,
                Phone = student.User.PhoneNumber,
                Address = student.Address,
                Birthday = student.Birthday,
                Age = DateUtils.CalculateAge(student.Birthday),
                Gender = student.Gender,
                ActiveStatus = student.User.ActiveStatus,
                PaymentStatus = paymentStatus,
                ImagePath = student.User.ProfileImagePath,
                AverageGrade = averageGrade,
                GroupsCount = studentGroups.Count,
                Groups = groupInfos,
                RecentGrades = recentGrades,
                RecentExams = examDtos
            };

            return new Response<GetStudentDetailedDto>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Детальная информация о студенте получена успешно",
                Data = studentDetailed
            };
        }
        catch (Exception ex)
        {
            return new Response<GetStudentDetailedDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Ошибка при получении детальной информации о студенте: {ex.Message}"
            };
        }
    }
}