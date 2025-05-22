using System.Net;
using Domain.DTOs.EmailDTOs;
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
using MimeKit.Text;

namespace Infrastructure.Services;

public class StudentService(
    DataContext context, IHttpContextAccessor httpContextAccessor,
    UserManager<User> userManager, string uploadPath,
    IEmailService emailService) : IStudentService
{
    private readonly string[] _allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif",".svg" };
    private const long MaxImageSize = 50 * 1024 * 1024; 
    
    

    public async Task SendLoginDetailsEmail(string email, string username, string password)
    {
        try
        {
            // Используем общий метод из EmailTemplateHelper для генерации HTML-шаблона письма
            string messageText = "Аккаунти шумо дар системаи мо сохта шуд. Барои Ворид ба система, аз чунин маълумоти воридшавӣ истифода кунед:";
            
            // Для студентов используем голубую цветовую схему
            string primaryColor = "#5E60CE";
            string accentColor = "#4EA8DE";
            
            var emailContent = Infrastructure.Helpers.EmailTemplateHelperNew.GenerateLoginEmailTemplate(
                username,
                password,
                messageText,
                primaryColor,
                accentColor,
                "Student"
            );

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
        // Формирование имени пользователя на основе номера телефона
        string username = createStudentDto.PhoneNumber.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");
        // Удаление международного кода, если он есть
        if (username.StartsWith("+"))
        {
            username = username.Substring(1);
        }
        
        // Проверка и обеспечение уникальности имени пользователя
        var existingUserWithSameUsername = await userManager.FindByNameAsync(username);
        int counter = 0;
        string originalUsername = username;
        
        // Если имя пользователя уже существует, добавляем цифры
        while (existingUserWithSameUsername != null)
        {
            counter++;
            username = originalUsername + counter;
            existingUserWithSameUsername = await userManager.FindByNameAsync(username);
        }

        var profileImagePath = string.Empty;
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
        
        // Handle document file upload
        string documentPath = string.Empty;
        if (createStudentDto.DocumentFile != null && createStudentDto.DocumentFile.Length > 0)
        {
            var fileExtension = Path.GetExtension(createStudentDto.DocumentFile.FileName).ToLowerInvariant();
            // Allowed document formats: .pdf, .doc, .docx, .jpg, .jpeg, .png
            string[] allowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
            
            if (!allowedDocumentExtensions.Contains(fileExtension))
                return new Response<string>(HttpStatusCode.BadRequest,
                    "Invalid document format. Allowed formats: .pdf, .doc, .docx, .jpg, .jpeg, .png");

            // Maximum document size: 20 MB
            const long maxDocumentSize = 20 * 1024 * 1024;
            if (createStudentDto.DocumentFile.Length > maxDocumentSize)
                return new Response<string>(HttpStatusCode.BadRequest, "Document size must be less than 20MB");

            var documentsFolder = Path.Combine(uploadPath, "uploads", "documents", "student");
            if (!Directory.Exists(documentsFolder))
                Directory.CreateDirectory(documentsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(documentsFolder, uniqueFileName);

            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await createStudentDto.DocumentFile.CopyToAsync(fileStream);
            }

            documentPath = $"/uploads/documents/student/{uniqueFileName}";
        }

        var age = DateUtils.CalculateAge(createStudentDto.Birthday);

        
        var user = new User
        {
            UserName = username, // Используем сгенерированное имя пользователя на основе телефона
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
        var password = PasswordUtils.GenerateRandomPassword();
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return new Response<string>(HttpStatusCode.BadRequest, 
                string.Join(", ", result.Errors.Select(e => e.Description)));
        
        // Назначаем роль студента
        await userManager.AddToRoleAsync(user, Roles.Student);

       
        if (!string.IsNullOrEmpty(createStudentDto.Email))
        {
            // Отправляем письмо с новым username на основе телефона
            await SendLoginDetailsEmail(createStudentDto.Email, username, password);
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
    #endregion

    #region UpdateStudentAsync
    public async Task<Response<string>> UpdateStudentAsync(int id, UpdateStudentDto updateStudentDto)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, "Student not found");
        var age = DateUtils.CalculateAge(updateStudentDto.Birthday);

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
                UserId = s.UserId ,
                ImagePath = s.ProfileImage,
                Document = s.Document
            });
            
        var students = await studentsQuery.ToListAsync();
        
        // Добавление ролей для каждого студента
        foreach (var student in students)
        {
            if (student.UserId > 0)
            {
                var user = await userManager.FindByIdAsync(student.UserId.ToString());
                if (user != null)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    student.Role = roles.FirstOrDefault() ?? "Student"; // По умолчанию Student
                }
            }
        }

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
                ImagePath = s.ProfileImage,
                Document = s.Document
            })
            .FirstOrDefaultAsync();

        return student != null
            ? new Response<GetStudentDto>(student)
            : new Response<GetStudentDto>(HttpStatusCode.NotFound, "Student not found");
    }
    #endregion

    #region GetStudentsPagination

    public async Task<Response<string>> UpdateStudentDocumentAsync(int studentId, IFormFile? documentFile)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, "Student not found");

        if (documentFile == null)
            return new Response<string>(HttpStatusCode.BadRequest, "No document file provided");

        var fileExtension = Path.GetExtension(documentFile.FileName).ToLowerInvariant();
        // Allowed document formats: .pdf, .doc, .docx, .jpg, .jpeg, .png
        string[] allowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
            
        if (!allowedDocumentExtensions.Contains(fileExtension))
            return new Response<string>(HttpStatusCode.BadRequest,
                "Invalid document format. Allowed formats: .pdf, .doc, .docx, .jpg, .jpeg, .png");

        // Maximum document size: 20 MB
        const long maxDocumentSize = 20 * 1024 * 1024;
        if (documentFile.Length > maxDocumentSize)
            return new Response<string>(HttpStatusCode.BadRequest, "Document size must be less than 20MB");
            
        // Delete old document if it exists
        if (!string.IsNullOrEmpty(student.Document))
        {
            string oldDocumentPath;
            if (!Path.IsPathRooted(student.Document))
            {
                var relativePath = student.Document.TrimStart('/');
                oldDocumentPath = Path.Combine(uploadPath, relativePath);
            }
            else
            {
                oldDocumentPath = student.Document;
            }

            if (File.Exists(oldDocumentPath))
            {
                try
                {
                    File.Delete(oldDocumentPath);
                }
                catch
                {
                    // Ignore error if file can't be deleted
                }
            }
        }

        // Create directory for documents if it doesn't exist
        var documentsFolder = Path.Combine(uploadPath, "uploads", "documents", "student");
        if (!Directory.Exists(documentsFolder))
            Directory.CreateDirectory(documentsFolder);

        // Save new document
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(documentsFolder, uniqueFileName);

        await using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await documentFile.CopyToAsync(fileStream);
        }

        // Update document path in database
        student.Document = $"/uploads/documents/student/{uniqueFileName}";
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
        try
        {
            // Find the student in database
            var student = await context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            
            if (student == null)
                return new Response<byte[]>(HttpStatusCode.NotFound, "Student not found");
                
            // Check if the student has a document
            if (string.IsNullOrEmpty(student.Document))
                return new Response<byte[]>(HttpStatusCode.NotFound, "Student document not found");
            
            var filePath = Path.Combine(uploadPath, student.Document.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            
            if (!File.Exists(filePath))
                return new Response<byte[]>(HttpStatusCode.NotFound, "Document file not found on server");
                
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            return new Response<byte[]>(fileBytes);
        }
        catch (Exception ex)
        {
            return new Response<byte[]>(HttpStatusCode.InternalServerError, $"Error retrieving document: {ex.Message}");
        }
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
    #endregion
}