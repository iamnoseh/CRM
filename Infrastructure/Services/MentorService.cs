using System.Net;
using Domain.DTOs.EmailDTOs;
using Domain.DTOs.Mentor;
using Domain.Entities;
using Domain.Enums;
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

public class MentorService(
    DataContext context,
    UserManager<User> userManager,
    string uploadPath,
    IEmailService emailService,
    IHttpContextAccessor httpContextAccessor) : IMentorService
{
    private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private const long MaxImageSize = 50 * 1024 * 1024; 

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
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
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
                            <p>Your mentor account has been successfully created in our CRM system. Below are your login credentials:</p>
                            

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
                "Your Mentor Account Details",
                emailContent
            );

            await emailService.SendEmail(emailMessage, TextFormat.Html);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }
    }

    #region CreateMentorAsync

    public async Task<Response<string>> CreateMentorAsync(CreateMentorDto createMentorDto)
    {
        try
        {
            var existingUser = await userManager.FindByEmailAsync(createMentorDto.Email);
            if (existingUser != null)
                return new Response<string>(HttpStatusCode.BadRequest, "Пользователь с таким email уже существует");
            var center =
                await context.Centers.FirstOrDefaultAsync(c => c.Id == createMentorDto.CenterId && !c.IsDeleted);
            if (center == null)
                return new Response<string>(HttpStatusCode.NotFound, "Центр не найден");

            string profileImagePath = string.Empty;
            if (createMentorDto.ProfileImage != null && createMentorDto.ProfileImage.Length > 0)
            {
                var fileExtension = Path.GetExtension(createMentorDto.ProfileImage.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(fileExtension))
                    return new Response<string>(HttpStatusCode.BadRequest,
                        "Недопустимый формат изображения. Разрешенные форматы: .jpg, .jpeg, .png, .gif");

                if (createMentorDto.ProfileImage.Length > MaxImageSize)
                    return new Response<string>(HttpStatusCode.BadRequest,
                        "Размер изображения не должен превышать 10МБ");

                var profilesFolder = Path.Combine(uploadPath, "uploads", "mentor");
                if (!Directory.Exists(profilesFolder))
                    Directory.CreateDirectory(profilesFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(profilesFolder, uniqueFileName);

                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await createMentorDto.ProfileImage.CopyToAsync(fileStream);
                }

                profileImagePath = $"/uploads/mentor/{uniqueFileName}";
            }
            
            var age = CalculateAge(createMentorDto.Birthday);

           
            var user = new User
            {
                UserName = createMentorDto.Email,
                Email = createMentorDto.Email,
                PhoneNumber = createMentorDto.PhoneNumber,
                FullName = createMentorDto.FullName,
                Birthday = createMentorDto.Birthday,
                Age = age,
                Gender = createMentorDto.Gender,
                Address = createMentorDto.Address,
                CenterId = createMentorDto.CenterId,
                ProfileImagePath = profileImagePath,
                ActiveStatus = ActiveStatus.Active,
                PaymentStatus = createMentorDto.PaymentStatus,
            };
            
            var password = GenerateRandomPassword();
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return new Response<string>(HttpStatusCode.BadRequest,
                    string.Join(", ", result.Errors.Select(e => e.Description)));

            // Назначаем роль ментора
            await userManager.AddToRoleAsync(user, Role.Teacher.ToString());

            if (!string.IsNullOrEmpty(createMentorDto.Email))
            {
                await SendLoginDetailsEmail(createMentorDto.Email, createMentorDto.Email, password);
            }
            
            var mentor = new Mentor
            {
                FullName = createMentorDto.FullName,
                Email = createMentorDto.Email,
                Address = createMentorDto.Address,
                PhoneNumber = createMentorDto.PhoneNumber,
                Birthday = createMentorDto.Birthday,
                Age = age,
                Gender = createMentorDto.Gender,
                CenterId = createMentorDto.CenterId,
                UserId = user.Id,
                Experience = createMentorDto.Experience,
                Salary = createMentorDto.Salary,
                ProfileImage = profileImagePath,
                ActiveStatus = ActiveStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                PaymentStatus = createMentorDto.PaymentStatus,
            };

            await context.Mentors.AddAsync(mentor);
            var res = await context.SaveChangesAsync();

            return res > 0
                ? new Response<string>(HttpStatusCode.Created, "Ментор успешно создан")
                : new Response<string>(HttpStatusCode.BadRequest, "Не удалось создать ментора");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                $"Ошибка при создании ментора: {ex.Message}");
        }
    }

    #endregion

    public async Task<Response<string>> UpdateMentorAsync(int id, UpdateMentorDto updateMentorDto)
    {
        try
        {
            // Находим пользователя по ID
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
                return new Response<string>(HttpStatusCode.NotFound, "Ментор не найден");

            if (user.Email != updateMentorDto.Email)
            {
                var existingUser = await userManager.FindByEmailAsync(updateMentorDto.Email);
                if (existingUser != null && existingUser.Id != id)
                    return new Response<string>(HttpStatusCode.BadRequest, "Пользователь с таким email уже существует");

                user.Email = updateMentorDto.Email;
                user.UserName = updateMentorDto.Email;
            }
            
            if (user.CenterId != updateMentorDto.CenterId)
            {
                var center =
                    await context.Centers.FirstOrDefaultAsync(c => c.Id == updateMentorDto.CenterId && !c.IsDeleted);
                if (center == null)
                    return new Response<string>(HttpStatusCode.NotFound, "Центр не найден");

                user.CenterId = updateMentorDto.CenterId;
            }
            
            user.PhoneNumber = updateMentorDto.PhoneNumber;
            user.FullName = updateMentorDto.FullName;
            user.Address = updateMentorDto.Address;
            user.Birthday = updateMentorDto.Birthday;
            user.Gender = updateMentorDto.Gender;
            user.ActiveStatus = updateMentorDto.ActiveStatus;
            user.PaymentStatus = updateMentorDto.PaymentStatus;
            user.Salary = updateMentorDto.Salary;
            user.UpdatedAt = DateTime.UtcNow;

            if (updateMentorDto.ProfileImage != null && updateMentorDto.ProfileImage.Length > 0)
            {
                var fileExtension = Path.GetExtension(updateMentorDto.ProfileImage.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(fileExtension))
                    return new Response<string>(HttpStatusCode.BadRequest,
                        "Недопустимый формат изображения. Разрешенные форматы: .jpg, .jpeg, .png, .gif");

                if (updateMentorDto.ProfileImage.Length > MaxImageSize)
                    return new Response<string>(HttpStatusCode.BadRequest, "Размер изображения не должен превышать 10МБ");

                if (!string.IsNullOrEmpty(user.ProfileImagePath))
                {
                    var oldImagePath = Path.Combine(uploadPath, user.ProfileImagePath.TrimStart('/'));
                    if (File.Exists(oldImagePath))
                    {
                        try
                        {
                            File.Delete(oldImagePath);
                        }
                        catch
                        {
                            
                        }
                    }
                }

       
                var profilesFolder = Path.Combine(uploadPath, "uploads", "mentor");
                if (!Directory.Exists(profilesFolder))
                    Directory.CreateDirectory(profilesFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(profilesFolder, uniqueFileName);

                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await updateMentorDto.ProfileImage.CopyToAsync(fileStream);
                }

                var profileImagePath = $"/uploads/mentor/{uniqueFileName}";
                user.ProfileImagePath = profileImagePath;
            }

            context.Users.Update(user);
            var result = await context.SaveChangesAsync();

            if (result > 0)
                return new Response<string>(HttpStatusCode.OK, "Ментор успешно обновлен");
            else
                return new Response<string>(HttpStatusCode.InternalServerError, "Не удалось обновить ментора");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                $"Ошибка при обновлении ментора: {ex.Message}");
        }
    }

    public async Task<Response<string>> DeleteMentorAsync(int id)
    {
        try
        {
            var mentor = await context.Mentors
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Ментор не найден");
            
            mentor.IsDeleted = true;
            mentor.UpdatedAt = DateTime.UtcNow;
            context.Mentors.Update(mentor);
            
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == mentor.UserId && !u.IsDeleted);

            if (user != null)
            {
                user.IsDeleted = true;
                user.UpdatedAt = DateTime.UtcNow;
                context.Users.Update(user);
            }
            
            var mentorGroups = await context.MentorGroups
                .Where(mg => mg.MentorId == id && !mg.IsDeleted)
                .ToListAsync();

            foreach (var mentorGroup in mentorGroups)
            {
                mentorGroup.IsActive = false;
                mentorGroup.IsDeleted = true;
                mentorGroup.UpdatedAt = DateTime.UtcNow;
            }

            if (mentorGroups.Any())
                context.MentorGroups.UpdateRange(mentorGroups);

            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Ментор успешно удален")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось удалить ментора");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                $"Ошибка при удалении ментора: {ex.Message}");
        }
    }

    #region Get Methods

    public async Task<Response<List<GetMentorDto>>> GetMentors()
    {
        try
        {
            var mentorsQuery = context.Mentors
                .Where(m => !m.IsDeleted)
                .Select(m => new GetMentorDto
                {
                    Id = m.Id,
                    FullName = m.FullName,
                    Email = m.Email,
                    Phone = m.PhoneNumber,
                    Address = m.Address,
                    Birthday = m.Birthday,
                    Age = m.Age,
                    Salary = m.Salary,
                    Experience = m.Experience,
                    Gender = m.Gender,
                    ActiveStatus = m.ActiveStatus,
                    ImagePath = m.ProfileImage,
                    CenterId = m.CenterId,
                    UserId = m.UserId
                });
            
            var mentors = await mentorsQuery.ToListAsync();
            
            foreach (var mentor in mentors)
            {
                if (mentor.UserId > 0)
                {
                    var user = await userManager.FindByIdAsync(mentor.UserId.ToString());
                    if (user != null)
                    {
                        var roles = await userManager.GetRolesAsync(user);
                        mentor.Role = roles.FirstOrDefault() ?? "Teacher";
                    }
                }
            }

            if (mentors.Count == 0)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "Менторы не найдены");

            return new Response<List<GetMentorDto>>(mentors);
        }
        catch (Exception ex)
        {
            return new Response<List<GetMentorDto>>(HttpStatusCode.InternalServerError,
                $"Ошибка при получении менторов: {ex.Message}");
        }
    }

    public async Task<Response<GetMentorDto>> GetMentorByIdAsync(int id)
    {
        try
        {
            var mentor = await context.Mentors
                .Where(m => m.Id == id && !m.IsDeleted)
                .Select(m => new GetMentorDto
                {
                    Id = m.Id,
                    FullName = m.FullName,
                    Email = m.Email,
                    Phone = m.PhoneNumber,
                    Address = m.Address,
                    Birthday = m.Birthday,
                    Age = m.Age,
                    Salary = m.Salary,
                    Gender = m.Gender,
                    Experience = m.Experience,
                    PaymentStatus = m.PaymentStatus,
                    ActiveStatus = m.ActiveStatus,
                    ImagePath = m.ProfileImage,
                    CenterId = m.CenterId,
                    UserId = m.UserId,
                })
                .FirstOrDefaultAsync();

            if (mentor == null)
                return new Response<GetMentorDto>(HttpStatusCode.NotFound, "Ментор не найден");

            return new Response<GetMentorDto>(mentor);
        }
        catch (Exception ex)
        {
            return new Response<GetMentorDto>(HttpStatusCode.InternalServerError,
                $"Ошибка при получении ментора: {ex.Message}");
        }
    }


    #endregion

    #region UpdateUserProfileImageAsync

    public async Task<Response<string>> UpdateUserProfileImageAsync(int id, IFormFile? profileImage)
    {
        try
        {
            // Находим ментора по ID
            var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Ментор не найден");

            // Проверяем, что изображение предоставлено
            if (profileImage == null || profileImage.Length == 0)
                return new Response<string>(HttpStatusCode.BadRequest, "Изображение не предоставлено");
                
            // Проверка формата и размера изображения
            var fileExtension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
            if (!_allowedImageExtensions.Contains(fileExtension))
                return new Response<string>(HttpStatusCode.BadRequest,
                    "Недопустимый формат изображения. Разрешенные форматы: .jpg, .jpeg, .png, .gif");

            if (profileImage.Length > MaxImageSize)
                return new Response<string>(HttpStatusCode.BadRequest, "Размер изображения не должен превышать 10МБ");
                
            // Создаем директорию, если она не существует
            var profilesFolder = Path.Combine(uploadPath, "uploads", "mentor");
            if (!Directory.Exists(profilesFolder))
                Directory.CreateDirectory(profilesFolder);
                
            // Удаляем старое изображение, если оно существует
            if (!string.IsNullOrEmpty(mentor.ProfileImage))
            {
                var oldImagePath = Path.Combine(uploadPath, mentor.ProfileImage.TrimStart('/'));
                if (File.Exists(oldImagePath))
                {
                    try
                    {
                        File.Delete(oldImagePath);
                    }
                    catch
                    {
                        // Игнорируем ошибки при удалении старого файла
                    }
                }
            }
            
            // Сохраняем новое изображение
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(profilesFolder, uniqueFileName);
            
            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await profileImage.CopyToAsync(fileStream);
            }
            
            var profileImagePath = $"/uploads/mentor/{uniqueFileName}";

            // Обновляем ментора
            mentor.ProfileImage = profileImagePath;
            mentor.UpdatedAt = DateTime.UtcNow;
            context.Mentors.Update(mentor);
            
            // Обновляем связанного пользователя
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == mentor.UserId && !u.IsDeleted);
            if (user != null)
            {
                user.ProfileImagePath = profileImagePath;
                user.UpdatedAt = DateTime.UtcNow;
                context.Users.Update(user);
            }
            
            var result = await context.SaveChangesAsync();
            
            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Изображение профиля успешно обновлено")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось обновить изображение профиля");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Ошибка при обновлении изображения профиля: {ex.Message}");
        }
    }

    #endregion

    public async Task<PaginationResponse<List<GetMentorDto>>> GetMentorsPagination(MentorFilter filter)
    {
        try
        {
            var query = context.Mentors
                .Where(u =>
                    !u.IsDeleted);

 
            if (!string.IsNullOrEmpty(filter.FullName))
                query = query.Where(s => s.FullName.Contains(filter.FullName));

            if (!string.IsNullOrEmpty(filter.PhoneNumber))
                query = query.Where(s => s.Email.Contains(filter.PhoneNumber));
            

            if (filter.Age.HasValue)
                query = query.Where(s => s.CenterId == filter.Age.Value);

            if (filter.Gender.HasValue)
                query = query.Where(s => s.Gender == filter.Gender.Value);
            
            var totalCount = await query.CountAsync();
            
            var mentors = await query
                .OrderBy(u => u.FullName)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(u => new GetMentorDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    Address = u.Address,
                    Birthday = u.Birthday,
                    Age = CalculateAge(u.Birthday),
                    Gender = u.Gender,
                    ActiveStatus = u.ActiveStatus,
                    PaymentStatus = u.PaymentStatus,
                    ImagePath = u.ProfileImage,
                    Experience = u.Experience,
                    Salary = u.Salary,
                    UserId = u.UserId,
                    CenterId = u.CenterId,
                })
                .ToListAsync();

            if (mentors.Count == 0 && filter.PageNumber > 1)
                return new PaginationResponse<List<GetMentorDto>>(HttpStatusCode.NotFound, "Менторы не найдены");

            // Создаем ответ с пагинацией
            return new PaginationResponse<List<GetMentorDto>>
            {
                Data = mentors,
                StatusCode = (int)HttpStatusCode.OK,
                PageSize = filter.PageSize,
                PageNumber = filter.PageNumber,
                TotalRecords = totalCount
            };
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetMentorDto>>(HttpStatusCode.InternalServerError,
                $"Ошибка при получении менторов: {ex.Message}");
        }
    }

    public async Task<Response<List<GetMentorDto>>> GetMentorsByGroupAsync(int groupId)
    {
        try
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "Группа не найдена");

            var mentorIds = await context.MentorGroups
                .Where(mg => mg.GroupId == groupId && (bool)mg.IsActive && !mg.IsDeleted)
                .Select(mg => mg.MentorId)
                .ToListAsync();

            if (mentorIds.Count == 0)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound,
                    "Менторы не найдены для данной группы");

            var mentors = await context.Mentors
                .Where(u => mentorIds.Contains(u.Id) && !u.IsDeleted)
                .Select(u => new GetMentorDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    Address = u.Address,
                    Birthday = u.Birthday,
                    Age = CalculateAge(u.Birthday),
                    Gender = u.Gender,
                    ActiveStatus = u.ActiveStatus,
                    PaymentStatus = u.PaymentStatus,
                    ImagePath = u.ProfileImage,
                    Experience = u.Experience,
                    Salary = u.Salary,
                    UserId = u.UserId,
                    CenterId = u.CenterId
                })
                .ToListAsync();

            return new Response<List<GetMentorDto>>(mentors);
        }
        catch (Exception ex)
        {
            return new Response<List<GetMentorDto>>(HttpStatusCode.InternalServerError,
                $"Ошибка при получении менторов группы: {ex.Message}");
        }
    }

    public async Task<Response<List<GetMentorDto>>> GetMentorsByCourseAsync(int courseId)
    {
        try
        {
            var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);
            if (course == null)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "Курс не найден");
            var groupIds = await context.Groups
                .Where(g => g.CourseId == courseId && !g.IsDeleted)
                .Select(g => g.Id)
                .ToListAsync();

            if (groupIds.Count == 0)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "Группы не найдены для данного курса");

            var mentorIds = await context.MentorGroups
                .Where(mg => groupIds.Contains(mg.GroupId) && (bool)mg.IsActive && !mg.IsDeleted)
                .Select(mg => mg.MentorId)
                .Distinct()
                .ToListAsync();

            if (mentorIds.Count == 0)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound,
                    "Менторы не найдены для данного курса");

            var mentors = await context.Mentors
                .Where(u => mentorIds.Contains(u.Id) && !u.IsDeleted)
                .Select(u => new GetMentorDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    Address = u.Address,
                    Birthday = u.Birthday,
                    Age = CalculateAge(u.Birthday),
                    Gender = u.Gender,
                    ActiveStatus = u.ActiveStatus,
                    PaymentStatus = u.PaymentStatus,
                    ImagePath = u.ProfileImage,
                    Experience = u.Experience,
                    UserId = u.UserId,
                    Salary = u.Salary,
                    CenterId = u.CenterId
                })
                .ToListAsync();

            return new Response<List<GetMentorDto>>(mentors);
        }
        catch (Exception ex)
        {
            return new Response<List<GetMentorDto>>(HttpStatusCode.InternalServerError,
                $"Ошибка при получении менторов курса: {ex.Message}");
        }
    }
    
}