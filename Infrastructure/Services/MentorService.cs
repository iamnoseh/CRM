using System.Net;
using Domain.DTOs.Mentor;
using Domain.Entities;
using Domain.Enums;
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

public class MentorService(
    DataContext context,
    UserManager<User> userManager,
    string uploadPath,
    IEmailService emailService,
    IHttpContextAccessor httpContextAccessor) : IMentorService
{
    public async Task<Response<string>> CreateMentorAsync(CreateMentorDto createMentorDto)
    {
        try
        {
            var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == createMentorDto.CenterId && !c.IsDeleted);
            if (center == null)
                return new Response<string>(HttpStatusCode.NotFound, "Центр не найден");

            // Загрузка изображения профиля
            string profileImagePath = string.Empty;
            if (createMentorDto.ProfileImage != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(
                    createMentorDto.ProfileImage, uploadPath, "mentor", "profile");
                if (imageResult.StatusCode != 200)
                    return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);
                profileImagePath = imageResult.Data;
            }

            // Загрузка документа
            string documentPath = string.Empty;
            if (createMentorDto.DocumentFile != null)
            {
                var docResult = await FileUploadHelper.UploadFileAsync(
                    createMentorDto.DocumentFile, uploadPath, "mentor", "document");
                if (docResult.StatusCode != 200)
                    return new Response<string>((HttpStatusCode)docResult.StatusCode, docResult.Message);
                documentPath = docResult.Data;
            }

            // Создание пользователя
            var userResult = await UserManagementHelper.CreateUserAsync(
                createMentorDto,
                userManager,
                Roles.Teacher,
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
            if (!string.IsNullOrEmpty(createMentorDto.Email))
            {
                await EmailHelper.SendLoginDetailsEmailAsync(
                    emailService,
                    createMentorDto.Email,
                    username,
                    password,
                    "Mentor",
                    "#4776E6",
                    "#8E54E9");
            }

            // Создание ментора
            var mentor = new Mentor
            {
                FullName = createMentorDto.FullName,
                Email = createMentorDto.Email,
                Address = createMentorDto.Address,
                PhoneNumber = createMentorDto.PhoneNumber,
                Birthday = createMentorDto.Birthday,
                Age = DateUtils.CalculateAge(createMentorDto.Birthday),
                Gender = createMentorDto.Gender,
                CenterId = createMentorDto.CenterId,
                UserId = user.Id,
                Experience = createMentorDto.Experience,
                Salary = createMentorDto.Salary,
                ProfileImage = profileImagePath,
                Document = documentPath,
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
            return new Response<string>(HttpStatusCode.InternalServerError, $"Ошибка при создании ментора: {ex.Message}");
        }
    }

    public async Task<Response<string>> UpdateMentorAsync(int id, UpdateMentorDto updateMentorDto)
    {
        try
        {
            var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Ментор не найден");

            var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == updateMentorDto.CenterId && !c.IsDeleted);
            if (center == null)
                return new Response<string>(HttpStatusCode.NotFound, "Центр не найден");

            // Обновление изображения профиля
            if (updateMentorDto.ProfileImage != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(
                    updateMentorDto.ProfileImage, uploadPath, "mentor", "profile", true, mentor.ProfileImage);
                if (imageResult.StatusCode != 200)
                    return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);
                mentor.ProfileImage = imageResult.Data;
            }

            mentor.FullName = updateMentorDto.FullName;
            mentor.Email = updateMentorDto.Email;
            mentor.PhoneNumber = updateMentorDto.PhoneNumber;
            mentor.Address = updateMentorDto.Address;
            mentor.Birthday = updateMentorDto.Birthday;
            mentor.Age = DateUtils.CalculateAge(updateMentorDto.Birthday);
            mentor.Gender = updateMentorDto.Gender;
            mentor.Experience = updateMentorDto.Experience;
            mentor.Salary = updateMentorDto.Salary;
            mentor.ActiveStatus = updateMentorDto.ActiveStatus;
            mentor.PaymentStatus = updateMentorDto.PaymentStatus;
            mentor.UpdatedAt = DateTime.UtcNow;

            if (mentor.UserId != null)
            {
                var user = await userManager.FindByIdAsync(mentor.UserId.ToString());
                if (user != null)
                {
                    var updateResult = await UserManagementHelper.UpdateUserAsync(
                        user,
                        updateMentorDto,
                        userManager,
                        dto => dto.Email,
                        dto => dto.FullName,
                        dto => dto.PhoneNumber,
                        dto => dto.Birthday,
                        dto => dto.Gender,
                        dto => dto.Address,
                        dto => dto.ActiveStatus,
                        dto => updateMentorDto.CenterId,
                        dto => dto.PaymentStatus);
                    if (updateResult.StatusCode != 200)
                        return updateResult;
                }
            }

            context.Mentors.Update(mentor);
            var res = await context.SaveChangesAsync();

            return res > 0
                ? new Response<string>(HttpStatusCode.OK, "Ментор успешно обновлен")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось обновить ментора");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Ошибка при обновлении ментора: {ex.Message}");
        }
    }

    public async Task<Response<string>> DeleteMentorAsync(int id)
    {
        try
        {
            var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Ментор не найден");

            mentor.IsDeleted = true;
            mentor.UpdatedAt = DateTime.UtcNow;
            context.Mentors.Update(mentor);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == mentor.UserId && !u.IsDeleted);
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
            return new Response<string>(HttpStatusCode.InternalServerError, $"Ошибка при удалении ментора: {ex.Message}");
        }
    }

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
                    Document = m.Document,
                    CenterId = m.CenterId,
                    UserId = m.UserId,
                    
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
            return new Response<List<GetMentorDto>>(HttpStatusCode.InternalServerError, $"Ошибка при получении менторов: {ex.Message}");
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
                    Document = m.Document,
                    CenterId = m.CenterId,
                    UserId = m.UserId,
                })
                .FirstOrDefaultAsync();

            if (mentor == null)
                return new Response<GetMentorDto>(HttpStatusCode.NotFound, "Ментор не найден");

            if (mentor.UserId > 0)
            {
                var user = await userManager.FindByIdAsync(mentor.UserId.ToString());
                if (user != null)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    mentor.Role = roles.FirstOrDefault() ?? "Teacher";
                }
            }

            return new Response<GetMentorDto>(mentor);
        }
        catch (Exception ex)
        {
            return new Response<GetMentorDto>(HttpStatusCode.InternalServerError, $"Ошибка при получении ментора: {ex.Message}");
        }
    }

    public async Task<Response<string>> UpdateMentorDocumentAsync(int mentorId, IFormFile? documentFile)
    {
        var mentor = await context.Mentors.FirstOrDefaultAsync(s => s.Id == mentorId && !s.IsDeleted);
        if (mentor == null)
            return new Response<string>(HttpStatusCode.NotFound, "Ментор не найден");

        var docResult = await FileUploadHelper.UploadFileAsync(
            documentFile, uploadPath, "mentor", "document", true, mentor.Document);
        if (docResult.StatusCode != 200)
            return new Response<string>((HttpStatusCode)docResult.StatusCode, docResult.Message);

        mentor.Document = docResult.Data;
        mentor.UpdatedAt = DateTime.UtcNow;
        context.Mentors.Update(mentor);
        var res = await context.SaveChangesAsync();

        return res > 0
            ? new Response<string>(HttpStatusCode.OK, "Mentor document updated successfully")
            : new Response<string>(HttpStatusCode.BadRequest, "Failed to update mentor document");
    }

    public async Task<Response<byte[]>> GetMentorDocument(int mentorId)
    {
        var mentor = await context.Mentors.FirstOrDefaultAsync(s => s.Id == mentorId && !s.IsDeleted);
        if (mentor == null)
            return new Response<byte[]>(HttpStatusCode.NotFound, "Ментор не найден");

        return await FileUploadHelper.GetFileAsync(mentor.Document, uploadPath);
    }

    public async Task<PaginationResponse<List<GetMentorDto>>> GetMentorsPagination(MentorFilter filter)
    {
        try
        {
            var query = context.Mentors.Where(u => !u.IsDeleted);

            if (!string.IsNullOrEmpty(filter.FullName))
                query = query.Where(s => s.FullName.Contains(filter.FullName));

            if (!string.IsNullOrEmpty(filter.PhoneNumber))
                query = query.Where(s => s.PhoneNumber.Contains(filter.PhoneNumber));

            if (filter.Age.HasValue)
                query = query.Where(s => s.Age == filter.Age.Value);

            if (filter.Gender.HasValue)
                query = query.Where(s => s.Gender == filter.Gender.Value);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

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
                    Age = DateUtils.CalculateAge(u.Birthday),
                    Gender = u.Gender,
                    ActiveStatus = u.ActiveStatus,
                    PaymentStatus = u.PaymentStatus,
                    ImagePath = u.ProfileImage,
                    Experience = u.Experience,
                    Document = u.Document,
                    Salary = u.Salary,
                    UserId = u.UserId,
                    CenterId = u.CenterId,
                })
                .ToListAsync();

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

            if (mentors.Count == 0 && filter.PageNumber > 1)
                return new PaginationResponse<List<GetMentorDto>>(HttpStatusCode.NotFound, "Менторы не найдены");

            return new PaginationResponse<List<GetMentorDto>>
            {
                Data = mentors,
                StatusCode = (int)HttpStatusCode.OK,
                PageSize = filter.PageSize,
                PageNumber = filter.PageNumber,
                TotalRecords = totalCount,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetMentorDto>>(HttpStatusCode.InternalServerError, $"Ошибка при получении менторов: {ex.Message}");
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
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "Менторы не найдены для данной группы");

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
                    Age = DateUtils.CalculateAge(u.Birthday),
                    Gender = u.Gender,
                    ActiveStatus = u.ActiveStatus,
                    PaymentStatus = u.PaymentStatus,
                    ImagePath = u.ProfileImage,
                    Experience = u.Experience,
                    Document = u.Document,
                    Salary = u.Salary,
                    UserId = u.UserId,
                    CenterId = u.CenterId
                })
                .ToListAsync();

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

            return new Response<List<GetMentorDto>>(mentors);
        }
        catch (Exception ex)
        {
            return new Response<List<GetMentorDto>>(HttpStatusCode.InternalServerError, $"Ошибка при получении менторов группы: {ex.Message}");
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
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "Менторы не найдены для данного курса");

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
                    Age = DateUtils.CalculateAge(u.Birthday),
                    Gender = u.Gender,
                    ActiveStatus = u.ActiveStatus,
                    PaymentStatus = u.PaymentStatus,
                    ImagePath = u.ProfileImage,
                    Experience = u.Experience,
                    Document = u.Document,
                    UserId = u.UserId,
                    Salary = u.Salary,
                    CenterId = u.CenterId
                })
                .ToListAsync();

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

            return new Response<List<GetMentorDto>>(mentors);
        }
        catch (Exception ex)
        {
            return new Response<List<GetMentorDto>>(HttpStatusCode.InternalServerError, $"Ошибка при получении менторов курса: {ex.Message}");
        }
    }

    public async Task<Response<string>> UpdateUserProfileImageAsync(int id, IFormFile? profileImage)
    {
        var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
        if (mentor == null)
            return new Response<string>(HttpStatusCode.NotFound, "Ментор не найден");

        var imageResult = await FileUploadHelper.UploadFileAsync(
            profileImage, uploadPath, "mentor", "profile", true, mentor.ProfileImage);
        if (imageResult.StatusCode != 200)
            return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);

        mentor.ProfileImage = imageResult.Data;
        mentor.UpdatedAt = DateTime.UtcNow;

        if (mentor.UserId != null)
        {
            var user = await userManager.FindByIdAsync(mentor.UserId.ToString());
            if (user != null)
            {
                user.ProfileImagePath = mentor.ProfileImage;
                await userManager.UpdateAsync(user);
            }
        }

        context.Mentors.Update(mentor);
        var res = await context.SaveChangesAsync();

        return res > 0
            ? new Response<string>(HttpStatusCode.OK, "Profile image updated successfully")
            : new Response<string>(HttpStatusCode.BadRequest, "Failed to update profile image");
    }

    public async Task<Response<string>> UpdateMentorPaymentStatusAsync(int mentorId, Domain.Enums.PaymentStatus status)
    {
        var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == mentorId && !m.IsDeleted);
        if (mentor == null)
            return new Response<string>(System.Net.HttpStatusCode.NotFound, "Mentor not found");

        mentor.PaymentStatus = status;
        mentor.UpdatedAt = DateTime.UtcNow;
        context.Mentors.Update(mentor);

        if (mentor.UserId != null)
        {
            var user = await userManager.FindByIdAsync(mentor.UserId.ToString());
            if (user != null)
            {
                user.PaymentStatus = status;
                await userManager.UpdateAsync(user);
            }
        }

        var res = await context.SaveChangesAsync();
        return res > 0
            ? new Response<string>(System.Net.HttpStatusCode.BadRequest, "Failed to update payment status")
            : new Response<string>(System.Net.HttpStatusCode.OK, "Payment status updated successfully");
    }
}