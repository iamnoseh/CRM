using System.Net;
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

namespace Infrastructure.Services;

public class MentorService(
    DataContext context,
    IHttpContextAccessor httpContextAccessor,
    UserManager<User> userManager,
    string uploadPath,
    IEmailService emailService) : IMentorService
{
    private readonly string[] _allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
    private const long MaxImageSize = 50 * 1024 * 1024; // 50MB

    #region CreateMentorAsync
    public async Task<Response<string>> CreateMentorAsync(CreateMentorDto createMentorDto)
    {
        try
        {
            // Проверяем, существует ли пользователь с таким email
            var existingUser = await userManager.FindByEmailAsync(createMentorDto.Email);
            if (existingUser != null)
                return new Response<string>(HttpStatusCode.BadRequest, "Email already exists");

            var existingMentor = await context.Mentors.AnyAsync(m => m.Email == createMentorDto.Email);
            if (existingMentor)
                return new Response<string>(HttpStatusCode.BadRequest, "Mentor with this email already exists");

            // Проверяем, существует ли преподаватель с таким телефоном
            var existingPhone = await context.Mentors.AnyAsync(m => m.PhoneNumber == createMentorDto.PhoneNumber);
            if (existingPhone)
                return new Response<string>(HttpStatusCode.BadRequest, "Phone number already exists");

            // Обработка изображения профиля
            string profileImagePath = string.Empty;
            if (createMentorDto.ProfileImage != null)
            {
                var fileExtension = Path.GetExtension(createMentorDto.ProfileImage.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(fileExtension))
                    return new Response<string>(HttpStatusCode.BadRequest, 
                        "Invalid image format. Allowed formats: .jpg, .jpeg, .png, .gif");

                if (createMentorDto.ProfileImage.Length > MaxImageSize)
                    return new Response<string>(HttpStatusCode.BadRequest, 
                        "Image size must be less than 50MB");

                var profilesFolder = Path.Combine(uploadPath, "uploads", "mentors");
                if (!Directory.Exists(profilesFolder))
                    Directory.CreateDirectory(profilesFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(profilesFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await createMentorDto.ProfileImage.CopyToAsync(fileStream);
                }

                profileImagePath = $"/uploads/mentors/{uniqueFileName}";
            }

            // Создаем пользователя в Identity
            var user = new User
            {
                UserName = createMentorDto.Email,
                Email = createMentorDto.Email,
                PhoneNumber = createMentorDto.PhoneNumber,
                FullName = createMentorDto.FullName,
                Address = createMentorDto.Address,
                Gender = createMentorDto.Gender
            };

            string password = GeneratePassword(8);
                

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new Response<string>(HttpStatusCode.BadRequest, errors);
            }

            // Добавляем роль преподавателя
            await userManager.AddToRoleAsync(user, Roles.Teacher);

            // Создаем запись преподавателя
            var mentor = new Mentor
            {
                FullName = createMentorDto.FullName,
                Email = createMentorDto.Email,
                PhoneNumber = createMentorDto.PhoneNumber,
                Address = createMentorDto.Address,
                Salary = createMentorDto.Salary ?? 0,
                Birthday = createMentorDto.Birthday,
                Gender = createMentorDto.Gender,
                ActiveStatus = createMentorDto.ActiveStatus,
                PaymentStatus = createMentorDto.PaymentStatus,
                ProfileImage = profileImagePath,
                UserId = user.Id,
                CenterId = createMentorDto.CenterId
            };

            // Вычисляем возраст на основе даты рождения
            mentor.Age = CalculateAge(createMentorDto.Birthday);

            await context.Mentors.AddAsync(mentor);
            var saveResult = await context.SaveChangesAsync();

            if (saveResult > 0)
            {
                // Отправляем логин и пароль на email преподавателя
                string emailSubject = "Ваши учетные данные для входа в CRM";
                string emailBody = $@"
                <h2>Добро пожаловать в CRM систему!</h2>
                <p>Для Вас был создан аккаунт преподавателя. Ниже указаны данные для входа:</p>
                <p><strong>Логин:</strong> {createMentorDto.Email}</p>
                <p><strong>Пароль:</strong> {password}</p>
                <p>Пожалуйста, смените пароль при первом входе в систему.</p>
                <p>С уважением, администрация системы.</p>";

                await emailService.SendEmail(
                    new Domain.DTOs.EmailDTOs.EmailMessageDto(
                        new[] { createMentorDto.Email }, 
                        emailSubject, 
                        emailBody
                    ), 
                    MimeKit.Text.TextFormat.Html
                );
                
                return new Response<string>(HttpStatusCode.Created, "Mentor created successfully");
            }
            
            // Если не удалось сохранить преподавателя, удаляем созданного пользователя
            await userManager.DeleteAsync(user);
            return new Response<string>(HttpStatusCode.InternalServerError, "Failed to create mentor");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region UpdateMentorAsync
    public async Task<Response<string>> UpdateMentorAsync(int id, UpdateMentorDto updateMentorDto)
    {
        try
        {
            var mentor = await context.Mentors
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Mentor not found");

            // Проверяем, не занят ли email другим преподавателем
            if (mentor.Email != updateMentorDto.Email)
            {
                var existingEmail = await context.Mentors
                    .AnyAsync(m => m.Email == updateMentorDto.Email && m.Id != id);

                if (existingEmail)
                    return new Response<string>(HttpStatusCode.BadRequest, "Email already exists");
            }

            // Проверяем, не занят ли телефон другим преподавателем
            if (mentor.PhoneNumber != updateMentorDto.PhoneNumber)
            {
                var existingPhone = await context.Mentors
                    .AnyAsync(m => m.PhoneNumber == updateMentorDto.PhoneNumber && m.Id != id);

                if (existingPhone)
                    return new Response<string>(HttpStatusCode.BadRequest, "Phone number already exists");
            }

            // Обработка изображения профиля
            if (updateMentorDto.ProfileImage != null)
            {
                var fileExtension = Path.GetExtension(updateMentorDto.ProfileImage.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(fileExtension))
                    return new Response<string>(HttpStatusCode.BadRequest, 
                        "Invalid image format. Allowed formats: .jpg, .jpeg, .png, .gif");

                if (updateMentorDto.ProfileImage.Length > MaxImageSize)
                    return new Response<string>(HttpStatusCode.BadRequest, 
                        "Image size must be less than 50MB");

                // Удаляем старое изображение
                if (!string.IsNullOrEmpty(mentor.ProfileImage))
                {
                    var oldImagePath = Path.Combine(uploadPath, mentor.ProfileImage.TrimStart('/'));
                    if (File.Exists(oldImagePath))
                        File.Delete(oldImagePath);
                }

                var profilesFolder = Path.Combine(uploadPath, "uploads", "mentors");
                if (!Directory.Exists(profilesFolder))
                    Directory.CreateDirectory(profilesFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(profilesFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await updateMentorDto.ProfileImage.CopyToAsync(fileStream);
                }

                mentor.ProfileImage = $"/uploads/mentors/{uniqueFileName}";
            }

            // Обновляем данные пользователя
            var user = mentor.User;
            if (user != null)
            {
                user.FullName = updateMentorDto.FullName;
                user.Email = updateMentorDto.Email;
                user.UserName = updateMentorDto.Email;
                user.PhoneNumber = updateMentorDto.PhoneNumber;
                user.Address = updateMentorDto.Address;
                user.Gender = updateMentorDto.Gender;
                
                
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    return new Response<string>(HttpStatusCode.BadRequest, errors);
                }
            }

            // Обновляем данные преподавателя
            mentor.FullName = updateMentorDto.FullName;
            mentor.Email = updateMentorDto.Email;
            mentor.PhoneNumber = updateMentorDto.PhoneNumber;
            mentor.Address = updateMentorDto.Address;
            mentor.Salary = updateMentorDto.Salary ?? mentor.Salary;
            mentor.Birthday = updateMentorDto.Birthday;
            mentor.Gender = updateMentorDto.Gender;
            mentor.ActiveStatus = updateMentorDto.ActiveStatus;
            mentor.PaymentStatus = updateMentorDto.PaymentStatus;
            mentor.Age = CalculateAge(updateMentorDto.Birthday);
            mentor.UpdatedAt = DateTime.UtcNow;
            mentor.CenterId = updateMentorDto.CenterId;
            context.Mentors.Update(mentor);
            var saveResult = await context.SaveChangesAsync();

            return saveResult > 0 
                ? new Response<string>(HttpStatusCode.OK, "Mentor updated successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to update mentor");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region DeleteMentorAsync
    public async Task<Response<string>> DeleteMentorAsync(int id)
    {
        try
        {
            var mentor = await context.Mentors
                .Include(m => m.User)
                .Include(m => m.Groups)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Mentor not found");

            // Проверяем, есть ли у преподавателя активные группы
            if (mentor.Groups.Any(g => g.Status == ActiveStatus.Active))
                return new Response<string>(HttpStatusCode.BadRequest, 
                    "Cannot delete mentor with active groups. Please reassign the groups first.");

            // Выполняем мягкое удаление преподавателя
            mentor.IsDeleted = true;
            mentor.UpdatedAt = DateTime.UtcNow;

            // Если у пользователя есть профильное изображение, удаляем его
            if (!string.IsNullOrEmpty(mentor.ProfileImage))
            {
                var imagePath = Path.Combine(uploadPath, mentor.ProfileImage.TrimStart('/'));
                if (File.Exists(imagePath))
                    File.Delete(imagePath);
            }

            // Деактивируем связанный аккаунт пользователя
            if (mentor.User != null)
            {
                await userManager.SetLockoutEnabledAsync(mentor.User, true);
                await userManager.SetLockoutEndDateAsync(mentor.User, DateTimeOffset.MaxValue);
            }

            var result = await context.SaveChangesAsync();
            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Mentor deleted successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to delete mentor");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetMentors
    public async Task<Response<List<GetMentorDto>>> GetMentors()
    {
        try
        {
            var mentors = await context.Mentors
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
                    Gender = m.Gender,
                    ActiveStatus = m.ActiveStatus,
                    PaymentStatus = m.PaymentStatus,
                    ImagePath = m.ProfileImage,
                    Salary = m.Salary,
                    CenterId = m.CenterId,
                })
                .ToListAsync();

            return mentors.Any() 
                ? new Response<List<GetMentorDto>>(mentors)
                : new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "No mentors found");
        }
        catch (Exception ex)
        {
            return new Response<List<GetMentorDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetMentorByIdAsync
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
                    Gender = m.Gender,
                    ActiveStatus = m.ActiveStatus,
                    PaymentStatus = m.PaymentStatus,
                    ImagePath = m.ProfileImage,
                    Salary = m.Salary,
                    CenterId = m.CenterId,
                })
                .FirstOrDefaultAsync();

            return mentor != null 
                ? new Response<GetMentorDto>(mentor)
                : new Response<GetMentorDto>(HttpStatusCode.NotFound, "Mentor not found");
        }
        catch (Exception ex)
        {
            return new Response<GetMentorDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region UpdateUserProfileImageAsync
    public async Task<Response<string>> UpdateUserProfileImageAsync(int id, IFormFile? profileImage)
    {
        try
        {
            var mentor = await context.Mentors
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Mentor not found");

            if (profileImage == null)
                return new Response<string>(HttpStatusCode.BadRequest, "No image provided");

            var fileExtension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
            if (!_allowedImageExtensions.Contains(fileExtension))
                return new Response<string>(HttpStatusCode.BadRequest, 
                    "Invalid image format. Allowed formats: .jpg, .jpeg, .png, .gif");

            if (profileImage.Length > MaxImageSize)
                return new Response<string>(HttpStatusCode.BadRequest, 
                    "Image size must be less than 50MB");

            // Удаляем старое изображение
            if (!string.IsNullOrEmpty(mentor.ProfileImage))
            {
                var oldImagePath = Path.Combine(uploadPath, mentor.ProfileImage.TrimStart('/'));
                if (File.Exists(oldImagePath))
                    File.Delete(oldImagePath);
            }

            var profilesFolder = Path.Combine(uploadPath, "uploads", "mentors");
            if (!Directory.Exists(profilesFolder))
                Directory.CreateDirectory(profilesFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(profilesFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await profileImage.CopyToAsync(fileStream);
            }

            mentor.ProfileImage = $"/uploads/mentors/{uniqueFileName}";
            mentor.UpdatedAt = DateTime.UtcNow;

            context.Mentors.Update(mentor);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Profile image updated successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to update profile image");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetMentorsPagination
    public async Task<PaginationResponse<List<GetMentorDto>>> GetMentorsPagination(MentorFilter filter)
    {
        var query = context.Mentors
            .Where(m => !m.IsDeleted)
            .AsQueryable();

        // Применяем фильтры
        if (!string.IsNullOrWhiteSpace(filter.FullName))
            query = query.Where(m => m.FullName.Contains(filter.FullName));

        if (!string.IsNullOrWhiteSpace(filter.PhoneNumber))
            query = query.Where(m => m.PhoneNumber.Contains(filter.PhoneNumber));

        if (filter.Age.HasValue)
            query = query.Where(m => m.Age == filter.Age.Value);

        if (filter.Gender.HasValue)
            query = query.Where(m => m.Gender == filter.Gender.Value);

        if (filter.Salary.HasValue)
            query = query.Where(m => m.Salary == filter.Salary.Value);

        // Получаем общее количество результатов
        var totalRecords = await query.CountAsync();

        // Применяем пагинацию
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var mentors = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(filter.PageSize)
            .Select(m => new GetMentorDto
            {
                Id = m.Id,
                FullName = m.FullName,
                Email = m.Email,
                Phone = m.PhoneNumber,
                Address = m.Address,
                Birthday = m.Birthday,
                Age = m.Age,
                Gender = m.Gender,
                ActiveStatus = m.ActiveStatus,
                PaymentStatus = m.PaymentStatus,
                ImagePath = m.ProfileImage,
                Salary = m.Salary,
                CenterId = m.CenterId,
            })
            .ToListAsync();

        return new PaginationResponse<List<GetMentorDto>>(
            mentors,
            totalRecords,
            filter.PageNumber,
            filter.PageSize);
    }
    #endregion

    #region GetMentorsByGroupAsync
    public async Task<Response<List<GetMentorDto>>> GetMentorsByGroupAsync(int groupId)
    {
        try
        {
            var group = await context.Groups
                .Include(g => g.Mentor)
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

            if (group == null)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "Group not found");

            if (group.Mentor == null)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "No mentors assigned to this group");

            var mentorDto = new GetMentorDto
            {
                Id = group.Mentor.Id,
                FullName = group.Mentor.FullName,
                Email = group.Mentor.Email,
                Phone = group.Mentor.PhoneNumber,
                Address = group.Mentor.Address,
                Birthday = group.Mentor.Birthday,
                Age = group.Mentor.Age,
                Gender = group.Mentor.Gender,
                ActiveStatus = group.Mentor.ActiveStatus,
                PaymentStatus = group.Mentor.PaymentStatus,
                ImagePath = group.Mentor.ProfileImage,
                Salary = group.Mentor.Salary,
                CenterId = group.Mentor.CenterId,
            };

            return new Response<List<GetMentorDto>>(new List<GetMentorDto> { mentorDto });
        }
        catch (Exception ex)
        {
            return new Response<List<GetMentorDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetMentorsByCourseAsync
    public async Task<Response<List<GetMentorDto>>> GetMentorsByCourseAsync(int courseId)
    {
        try
        {
            var course = await context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

            if (course == null)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "Course not found");

    
            // Находим всех преподавателей, которые ведут группы данного курса
            var mentors = await context.Mentors
                .Where(m => !m.IsDeleted && m.Groups.Any(g => g.CourseId == courseId && !g.IsDeleted))
                .Select(m => new GetMentorDto
                {
                    Id = m.Id,
                    FullName = m.FullName,
                    Email = m.Email,
                    Phone = m.PhoneNumber,
                    Address = m.Address,
                    Birthday = m.Birthday,
                    Age = m.Age,
                    Gender = m.Gender,
                    ActiveStatus = m.ActiveStatus,
                    PaymentStatus = m.PaymentStatus,
                    ImagePath = m.ProfileImage,
                    Salary = m.Salary,
                    CenterId = m.CenterId,
                })
                .ToListAsync();

            return mentors.Any() 
                ? new Response<List<GetMentorDto>>(mentors)
                : new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "No mentors found for this course");
        }
        catch (Exception ex)
        {
            return new Response<List<GetMentorDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region Helper Methods
    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age))
            age--;
        return age;
    }

    private static string GeneratePassword(int length)
    {
        // Генерация случайного пароля указанной длины
        const string upperCase = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // без O и I, которые похожи на цифры
        const string lowerCase = "abcdefghijkmnpqrstuvwxyz"; // без l и o
        const string digits = "23456789";                    // без 0 и 1
        const string special = "-";
        
        var random = new Random();
        var chars = new char[length];
        
        // Обязательно добавляем одну заглавную букву
        chars[0] = upperCase[random.Next(upperCase.Length)];
        
        // Обязательно добавляем одну строчную букву
        chars[1] = lowerCase[random.Next(lowerCase.Length)];
        
        // Обязательно добавляем одну цифру
        chars[2] = digits[random.Next(digits.Length)];
        
        // Добавляем один специальный символ (дефис)
        chars[3] = special[0];
        
        // Заполняем остальные символы
        string allChars = upperCase + lowerCase + digits + special;
        for (int i = 4; i < length; i++)
        {
            chars[i] = allChars[random.Next(allChars.Length)];
        }
        
        // Перемешиваем символы для безопасности
        for (int i = 0; i < length; i++)
        {
            int j = random.Next(length);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        
        return new string(chars);
    }
    #endregion
}