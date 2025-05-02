using System.Net;
using Domain.DTOs.EmailDTOs;
using Domain.DTOs.Exam;
using Domain.DTOs.Grade;
using Domain.DTOs.Student;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Services.EmailService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MimeKit.Text;

namespace Infrastructure.Services;

public class StudentService(
    DataContext context, IHttpContextAccessor httpContextAccessor,
    UserManager<User> userManager, string uploadPath,
    IEmailService emailService) : IStudentService
{
    private readonly string[] _allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
    private const long MaxImageSize = 10 * 1024 * 1024; // 10MB
    
    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }

    private static string GenerateRandomPassword(int length = 8)
    {
        const string upperChars = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
        const string lowerChars = "abcde";
        const string numericChars = "0123456789";
        const string specialChars = "-.";
        
        var random = new Random();
        var chars = new List<char>();
        chars.Add(upperChars[random.Next(upperChars.Length)]);
        chars.Add(lowerChars[random.Next(lowerChars.Length)]);
        chars.Add(numericChars[random.Next(numericChars.Length)]);
        chars.Add(specialChars[random.Next(specialChars.Length)]);
        for (int i = chars.Count; i < length; i++)
        {
            var allChars = upperChars + lowerChars + numericChars + specialChars;
            chars.Add(allChars[random.Next(allChars.Length)]);
        }
        
        for (int i = 0; i < chars.Count; i++)
        {
            int swapIndex = random.Next(chars.Count);
            (chars[i], chars[swapIndex]) = (chars[swapIndex], chars[i]);
        }
        
        return new string(chars.ToArray());
    }

    private async Task SendLoginDetailsEmail(string email, string username, string password)
    {
        try
        {
            var emailContent = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #4CAF50; color: white; padding: 10px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .credentials {{ background-color: #efefef; padding: 15px; margin: 15px 0; border-left: 4px solid #4CAF50; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Welcome to Our CRM System</h2>
                        </div>
                        <div class='content'>
                            <p>Dear {email.Split('@')[0]},</p>
                            <p>Your account has been successfully created in our CRM system. Below are your login credentials:</p>
                            
                            <div class='credentials'>
                                <p><strong>Username:</strong> {username}</p>
                                <p><strong>Password:</strong> {password}</p>
                            </div>
                            
                            <p>Please keep this information secure and change your password after the first login.</p>
                            <p>If you have any questions, please contact our support team.</p>
                            <p>Thank you!</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message, please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>";

            var emailMessage = new EmailMessageDto(
                new List<string> { email },
                "Your Account Details",
                emailContent
            );
            
            await emailService.SendEmail(emailMessage, TextFormat.Html);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }
    }

    #region CreateStudentAsync
    public async Task<Response<string>> CreateStudentAsync(CreateStudentDto createStudentDto)
    {
        var existingUser = await userManager.FindByNameAsync(createStudentDto.PhoneNumber);
        if (existingUser != null)
            return new Response<string>(HttpStatusCode.BadRequest, "Username already exists");

        string profileImagePath = string.Empty;
        if (createStudentDto.ProfilePhoto != null && createStudentDto.ProfilePhoto.Length > 0)
        {
            var fileExtension = Path.GetExtension(createStudentDto.ProfilePhoto.FileName).ToLowerInvariant();
            if (!_allowedImageExtensions.Contains(fileExtension))
                return new Response<string>(HttpStatusCode.BadRequest,
                    "Invalid profile image format. Allowed formats: .jpg, .jpeg, .png, .gif");

            if (createStudentDto.ProfilePhoto.Length > MaxImageSize)
                return new Response<string>(HttpStatusCode.BadRequest, "Profile image size must be less than 10MB");

            var profilesFolder = Path.Combine(uploadPath, "uploads", "student");
            if (!Directory.Exists(profilesFolder))
                Directory.CreateDirectory(profilesFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(profilesFolder, uniqueFileName);

            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await createStudentDto.ProfilePhoto.CopyToAsync(fileStream);
            }

            profileImagePath = $"/uploads/student/{uniqueFileName}";
        }

        var age = CalculateAge(createStudentDto.Birthday);

        
        var user = new User
        {
            UserName = createStudentDto.PhoneNumber,
            PhoneNumber = createStudentDto.PhoneNumber,
            FullName = createStudentDto.FullName,
            Birthday = createStudentDto.Birthday,
            Age = age,
            Gender = createStudentDto.Gender,
            Address = createStudentDto.Address,
            Email = createStudentDto.Email,
            CenterId = createStudentDto.CenterId,
            ProfileImagePath = profileImagePath
        };
        var password = GenerateRandomPassword();
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return new Response<string>(HttpStatusCode.BadRequest, 
                string.Join(", ", result.Errors.Select(e => e.Description)));

       
        if (!string.IsNullOrEmpty(createStudentDto.Email))
        {
            await SendLoginDetailsEmail(createStudentDto.Email, createStudentDto.PhoneNumber, password);
        }

        var student = new Student
        {
            FullName = createStudentDto.FullName,
            Email = createStudentDto.Email,
            Address = createStudentDto.Address,
            PhoneNumber = createStudentDto.PhoneNumber,
            Birthday = createStudentDto.Birthday,
            Age = age, 
            Gender = createStudentDto.Gender,
            CenterId = createStudentDto.CenterId,
            UserId = user.Id,
            ProfileImage = profileImagePath,
            ActiveStatus = Domain.Enums.ActiveStatus.Active,
            PaymentStatus = Domain.Enums.PaymentStatus.Completed
        };

        await context.Students.AddAsync(student);
        var res = await context.SaveChangesAsync();

        return res > 0
            ? new Response<string>(HttpStatusCode.Created, "Student Created Successfully")
            : new Response<string>(HttpStatusCode.BadRequest, "Student Creation Failed");
    }
    #endregion

    #region UpdateStudentAsync
    public async Task<Response<string>> UpdateStudentAsync(int id, UpdateStudentDto updateStudentDto)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, "Student not found");
        var age = CalculateAge(updateStudentDto.Birthday);

        student.FullName = updateStudentDto.FullName;
        student.Email = updateStudentDto.Email;
        student.Address = updateStudentDto.Address;
        student.PhoneNumber = updateStudentDto.PhoneNumber;
        student.Birthday = updateStudentDto.Birthday;
        student.Age = age; 
        student.Gender = updateStudentDto.Gender;
        student.ActiveStatus = updateStudentDto.ActiveStatus;
        student.PaymentStatus = updateStudentDto.PaymentStatus;
        student.UpdatedAt = DateTime.UtcNow;

        if (student.UserId != null)
        {
            var user = await userManager.FindByIdAsync(student.UserId.ToString());
            if (user != null)
            {
                user.FullName = updateStudentDto.FullName;
                user.PhoneNumber = updateStudentDto.PhoneNumber;
                user.Email = updateStudentDto.Email;
                user.Address = updateStudentDto.Address;
                user.Birthday = updateStudentDto.Birthday;
                user.Age = age;
                user.Gender = updateStudentDto.Gender;
                
                await userManager.UpdateAsync(user);
            }
        }

        context.Students.Update(student);
        var res = await context.SaveChangesAsync();

        return res > 0
            ? new Response<string>(HttpStatusCode.OK, "Student Updated Successfully")
            : new Response<string>(HttpStatusCode.BadRequest, "Student Update Failed");
    }
    #endregion

    #region DeleteStudentAsync
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
    #endregion

    #region GetStudents
    public async Task<Response<List<GetStudentDto>>> GetStudents()
    {
        var students = await context.Students
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
                ImagePath = s.ProfileImage
            })
            .ToListAsync();

        return students.Any()
            ? new Response<List<GetStudentDto>>(students)
            : new Response<List<GetStudentDto>>(HttpStatusCode.NotFound, "No students found");
    }
    #endregion

    #region GetStudentByIdAsync
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
                ImagePath = s.ProfileImage
            })
            .FirstOrDefaultAsync();

        return student != null
            ? new Response<GetStudentDto>(student)
            : new Response<GetStudentDto>(HttpStatusCode.NotFound, "Student not found");
    }
    #endregion

    #region GetStudentsPagination
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
    #endregion

    #region UpdateUserProfileImageAsync
    public async Task<Response<string>> UpdateUserProfileImageAsync(int studentId, IFormFile? profileImage)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, "Student not found");

        if (profileImage == null)
            return new Response<string>(HttpStatusCode.BadRequest, "No profile image provided");

        var fileExtension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
        if (!_allowedImageExtensions.Contains(fileExtension))
            return new Response<string>(HttpStatusCode.BadRequest,
                "Invalid profile image format. Allowed formats: .jpg, .jpeg, .png, .gif");

        if (profileImage.Length > MaxImageSize)
            return new Response<string>(HttpStatusCode.BadRequest, "Profile image size must be less than 10MB");
        
        if (!string.IsNullOrEmpty(student.ProfileImage))
        {
            var oldImagePath = Path.Combine(uploadPath, student.ProfileImage.TrimStart('/'));
            if (File.Exists(oldImagePath))
                File.Delete(oldImagePath);
        }
        var profilesFolder = Path.Combine(uploadPath, "uploads", "student");
        if (!Directory.Exists(profilesFolder))
            Directory.CreateDirectory(profilesFolder);

        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(profilesFolder, uniqueFileName);

        await using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await profileImage.CopyToAsync(fileStream);
        }

        var newProfileImagePath = $"/uploads/student/{uniqueFileName}";
        student.ProfileImage = newProfileImagePath;
        student.UpdatedAt = DateTime.UtcNow;

        // Обновление изображения в профиле пользователя
        if (student.UserId != null)
        {
            var user = await userManager.FindByIdAsync(student.UserId.ToString());
            if (user != null)
            {
                user.ProfileImagePath = newProfileImagePath;
                await userManager.UpdateAsync(user);
            }
        }

        context.Students.Update(student);
        var res = await context.SaveChangesAsync();

        return res > 0
            ? new Response<string>(HttpStatusCode.OK, "Profile image updated successfully")
            : new Response<string>(HttpStatusCode.BadRequest, "Failed to update profile image");
    }
    #endregion

    #region GetStudentDetailedAsync
    public async Task<Response<GetStudentDetailedDto>> GetStudentDetailedAsync(int id)
    {
        var student = await context.Students
            .Include(s => s.Center)
            .Include(s => s.StudentGroups)
            .ThenInclude(sg => sg.Group)
            .ThenInclude(g => g.Course)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (student == null)
            return new Response<GetStudentDetailedDto>(HttpStatusCode.NotFound, "Student not found");

        // Получаем средний балл студента
        double averageGrade = 0;
        var grades = await context.Grades
            .Where(g => g.StudentId == id && !g.IsDeleted && g.Value.HasValue)
            .ToListAsync();
            
        var exams = await context.Exams
            .Where(e => e.StudentId == id && !e.IsDeleted && e.Value.HasValue)
            .ToListAsync();
        
        if (grades.Any() || exams.Any())
        {
            double totalSum = 0;
            int totalCount = 0;
            
            if (grades.Any())
            {
                totalSum += grades.Sum(g => g.Value!.Value);
                totalCount += grades.Count;
            }
            
            if (exams.Any())
            {
                totalSum += exams.Sum(e => e.Value!.Value);
                totalCount += exams.Count;
            }
            
            averageGrade = totalCount > 0 ? Math.Round(totalSum / totalCount, 2) : 0;
        }

        // Создаем DTO с детальной информацией
        var result = new GetStudentDetailedDto
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
            ImagePath = student.ProfileImage,
            
            // Статистика
            AverageGrade = averageGrade,
            GroupsCount = student.StudentGroups.Count(sg => !sg.IsDeleted),
            Groups = student.StudentGroups
                .Where(sg => !sg.IsDeleted && sg.Group != null && !sg.Group.IsDeleted)
                .Select(sg => 
                {
                    // Расчет среднего балла для группы
                    double groupAvg = 0;
                    var groupGrades = grades.Where(g => g.GroupId == sg.GroupId).ToList();
                    var groupExams = exams.Where(e => e.GroupId == sg.GroupId).ToList();
                    
                    int gradeCount = groupGrades.Count + groupExams.Count;
                    double gradeSum = 0;
                    
                    if (groupGrades.Any())
                        gradeSum += groupGrades.Sum(g => g.Value!.Value);
                        
                    if (groupExams.Any())
                        gradeSum += groupExams.Sum(e => e.Value!.Value);
                        
                    if (gradeCount > 0)
                        groupAvg = Math.Round(gradeSum / gradeCount, 2);

                    return new GetStudentDetailedDto.GroupInfo
                    {
                        GroupId = sg.GroupId,
                        GroupName = sg.Group.Name,
                        GroupAverageGrade = groupAvg
                    };
                })
                .ToList()
        };
        
        result.RecentGrades = await context.Grades
            .Where(g => g.StudentId == id && !g.IsDeleted && g.Value.HasValue)
            .OrderByDescending(g => g.CreatedAt)
            .Take(5)
            .Select(g => new GetGradeDto
            {
                Id = g.Id,
                StudentId = g.StudentId,
                GroupId = g.GroupId,
                LessonId = g.LessonId,
                Value = g.Value,
                BonusPoints = g.BonusPoints,
                WeekIndex = g.WeekIndex,
                CreatedAt = g.CreatedAt,
                Comment = g.Comment,
                
            })
            .ToListAsync();

        // Получаем последние экзамены
        result.RecentExams = await context.Exams
            .Where(e => e.StudentId == id && !e.IsDeleted && e.Value.HasValue)
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .Select(e => new GetExamDto
            {
                Id = e.Id,
                StudentId = e.StudentId,
                GroupId = e.GroupId,
                Value = e.Value,
                BonusPoints = e.BonusPoints,
                Comment = e.Comment,
                WeekIndex = e.WeekIndex,
                ExamDate = e.ExamDate,
            })
            .ToListAsync();

        return new Response<GetStudentDetailedDto>(result);
    }
    #endregion
}