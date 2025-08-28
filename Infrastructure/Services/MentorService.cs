using System.Net;
using Domain.DTOs.Mentor;
using Domain.DTOs.Student;
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
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<string>(HttpStatusCode.BadRequest, "CenterId дар токен ёфт нашуд");
            
            string profileImagePath = string.Empty;
            if (createMentorDto.ProfileImage != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(
                    createMentorDto.ProfileImage, uploadPath, "mentor", "profile");
                if (imageResult.StatusCode != (int)HttpStatusCode.OK)
                    return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);
                profileImagePath = imageResult.Data;
            }
            
            string documentPath = string.Empty;
            if (createMentorDto.DocumentFile != null)
            {
                var docResult = await FileUploadHelper.UploadFileAsync(
                    createMentorDto.DocumentFile, uploadPath, "mentor", "document");
                if (docResult.StatusCode != (int)HttpStatusCode.OK)
                    return new Response<string>((HttpStatusCode)docResult.StatusCode, docResult.Message);
                documentPath = docResult.Data;
            }
            
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
                dto => centerId.Value,
                _ => profileImagePath);
                
            if (userResult.StatusCode != (int)HttpStatusCode.OK)
                return new Response<string>((HttpStatusCode)userResult.StatusCode, userResult.Message);

            var (user, password, username) = userResult.Data;
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
            
            var mentor = new Mentor
            {
                FullName = createMentorDto.FullName,
                Email = createMentorDto.Email,
                Address = createMentorDto.Address,
                PhoneNumber = createMentorDto.PhoneNumber,
                Birthday = createMentorDto.Birthday,
                Age = DateUtils.CalculateAge(createMentorDto.Birthday),
                Gender = createMentorDto.Gender,
                CenterId = centerId.Value,
                UserId = user.Id,
                Experience = createMentorDto.Experience,
                Salary = createMentorDto.Salary,
                ProfileImage = profileImagePath,
                Document = documentPath,
                ActiveStatus = ActiveStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                PaymentStatus = PaymentStatus.Completed,
            };

            await context.Mentors.AddAsync(mentor);
            var res = await context.SaveChangesAsync();

            return res > 0
                ? new Response<string>(HttpStatusCode.Created, "Устод бо муваффақият сохта шуд")
                : new Response<string>(HttpStatusCode.BadRequest, "Хатогӣ ҳангоми сохтани устод");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми сохтани устод: {ex.Message}");
        }
    }

    public async Task<Response<string>> UpdateMentorAsync(int id, UpdateMentorDto updateMentorDto)
    {
        try
        {
            var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Устод ёфт нашуд");

            var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == updateMentorDto.CenterId && !c.IsDeleted);
            if (center == null)
                return new Response<string>(HttpStatusCode.NotFound, "Маркази таълимӣ ёфт нашуд");
                
            if (updateMentorDto.ProfileImage != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(
                    updateMentorDto.ProfileImage, uploadPath, "mentor", "profile", true, mentor.ProfileImage);
                if (imageResult.StatusCode != (int)HttpStatusCode.OK)
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
            mentor.UpdatedAt = DateTimeOffset.UtcNow;

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
                    if (updateResult.StatusCode != (int)HttpStatusCode.OK)
                        return updateResult;
                }
            }

            context.Mentors.Update(mentor);
            var res = await context.SaveChangesAsync();

            return res > 0
                ? new Response<string>(HttpStatusCode.OK, "Устод бо муваффақият навсозӣ шуд")
                : new Response<string>(HttpStatusCode.InternalServerError, "Хатогӣ ҳангоми навсозии устод");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми навсозии устод: {ex.Message}");
        }
    }

    public async Task<Response<string>> DeleteMentorAsync(int id)
    {
        try
        {
            var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Устод ёфт нашуд");

            mentor.IsDeleted = true;
            mentor.UpdatedAt = DateTimeOffset.UtcNow;
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
                mentorGroup.UpdatedAt = DateTimeOffset.UtcNow;
            }

            if (mentorGroups.Any())
                context.MentorGroups.UpdateRange(mentorGroups);

            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Устод бо муваффақият нест карда шуд")
                : new Response<string>(HttpStatusCode.InternalServerError, "Хатогӣ ҳангоми несткунии устод");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми несткунии устод: {ex.Message}");
        }
    }

    public async Task<Response<List<GetMentorDto>>> GetMentors()
    {
        var mentorsQuery = context.Mentors
            .Where(m => !m.IsDeleted);
        mentorsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            mentorsQuery, httpContextAccessor, m => m.CenterId);
        var mentors = await mentorsQuery.ToListAsync();
        var dtos = mentors.Select(m => new GetMentorDto
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
            UserId = m.UserId
        }).ToList();
        return new Response<List<GetMentorDto>>(dtos);
    }

    public async Task<Response<GetMentorDto>> GetMentorByIdAsync(int id)
    {
        var mentorsQuery = context.Mentors
            .Where(m => m.Id == id && !m.IsDeleted);
        mentorsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            mentorsQuery, httpContextAccessor, m => m.CenterId);
        var m = await mentorsQuery.FirstOrDefaultAsync();
        if (m == null)
            return new Response<GetMentorDto>(HttpStatusCode.NotFound, "Устод ёфт нашуд");
        var dto = new GetMentorDto
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
            UserId = m.UserId
        };
        return new Response<GetMentorDto>(dto);
    }

    public async Task<Response<string>> UpdateMentorDocumentAsync(int mentorId, IFormFile? documentFile)
    {
        var mentor = await context.Mentors.FirstOrDefaultAsync(s => s.Id == mentorId && !s.IsDeleted);
        if (mentor == null)
            return new Response<string>(HttpStatusCode.NotFound, "Устод ёфт нашуд");

        var docResult = await FileUploadHelper.UploadFileAsync(
            documentFile, uploadPath, "mentor", "document", true, mentor.Document);
        if (docResult.StatusCode != (int)HttpStatusCode.OK)
            return new Response<string>((HttpStatusCode)docResult.StatusCode, docResult.Message);

        mentor.Document = docResult.Data;
        mentor.UpdatedAt = DateTimeOffset.UtcNow;
        context.Mentors.Update(mentor);
        var res = await context.SaveChangesAsync();

        return res > 0
            ? new Response<string>(HttpStatusCode.OK, "Ҳуҷҷати устод бо муваффақият навсозӣ шуд")
            : new Response<string>(HttpStatusCode.BadRequest, "Хатогӣ ҳангоми навсозии ҳуҷҷати устод");
    }

    public async Task<Response<byte[]>> GetMentorDocument(int mentorId)
    {
        var mentor = await context.Mentors.FirstOrDefaultAsync(s => s.Id == mentorId && !s.IsDeleted);
        if (mentor == null)
            return new Response<byte[]>(HttpStatusCode.NotFound, "Устод ёфт нашуд");

        return await FileUploadHelper.GetFileAsync(mentor.Document, uploadPath);
    }

    public async Task<PaginationResponse<List<GetMentorDto>>> GetMentorsPagination(MentorFilter filter)
    {
        // Sanitize pagination inputs to avoid zero/negative values
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

        var mentorsQuery = context.Mentors.Where(m => !m.IsDeleted);
        mentorsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            mentorsQuery, httpContextAccessor, m => m.CenterId);

        if (!string.IsNullOrEmpty(filter.FullName))
            mentorsQuery = mentorsQuery.Where(m => m.FullName.ToLower().Contains(filter.FullName.ToLower()));
        if (!string.IsNullOrEmpty(filter.PhoneNumber))
            mentorsQuery = mentorsQuery.Where(m => m.PhoneNumber.ToLower().Contains(filter.PhoneNumber.ToLower()));
        if (filter.CenterId.HasValue)
            mentorsQuery = mentorsQuery.Where(m => m.CenterId == filter.CenterId.Value);
        if (filter.Age.HasValue)
            mentorsQuery = mentorsQuery.Where(m => m.Age == filter.Age.Value);
        if (filter.Gender.HasValue)
            mentorsQuery = mentorsQuery.Where(m => m.Gender == filter.Gender.Value);
        if (filter.Salary.HasValue)
            mentorsQuery = mentorsQuery.Where(m => m.Salary == filter.Salary.Value);

        var totalRecords = await mentorsQuery.CountAsync();
        var skip = (pageNumber - 1) * pageSize;

        var mentors = await mentorsQuery
            .OrderBy(m => m.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        var dtos = mentors.Select(m => new GetMentorDto
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
            UserId = m.UserId
        }).ToList();

        return new PaginationResponse<List<GetMentorDto>>(dtos, totalRecords, pageNumber, pageSize);
    }

    public async Task<Response<List<GetMentorDto>>> GetMentorsByGroupAsync(int groupId)
    {
        try
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "Гурӯҳ ёфт нашуд");

            var mentorIds = await context.MentorGroups
                .Where(mg => mg.GroupId == groupId && (bool)mg.IsActive && !mg.IsDeleted)
                .Select(mg => mg.MentorId)
                .ToListAsync();

            if (mentorIds.Count == 0)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "Устодон барои ин гурӯҳ ёфт нашуданд");

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
            return new Response<List<GetMentorDto>>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми гирифтани устодони гурӯҳ: {ex.Message}");
        }
    }

    public async Task<Response<List<GetMentorDto>>> GetMentorsByCourseAsync(int courseId)
    {
        try
        {
            var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);
            if (course == null)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "Курс ёфт нашуд");

            var groupIds = await context.Groups
                .Where(g => g.CourseId == courseId && !g.IsDeleted)
                .Select(g => g.Id)
                .ToListAsync();

            if (groupIds.Count == 0)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "Гурӯҳҳо барои ин курс ёфт нашуданд");

            var mentorIds = await context.MentorGroups
                .Where(mg => groupIds.Contains(mg.GroupId) && (bool)mg.IsActive && !mg.IsDeleted)
                .Select(mg => mg.MentorId)
                .Distinct()
                .ToListAsync();

            if (mentorIds.Count == 0)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, "Устодон барои ин курс ёфт нашуданд");

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
            return new Response<List<GetMentorDto>>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми гирифтани устодони курс: {ex.Message}");
        }
    }

    public async Task<Response<string>> UpdateUserProfileImageAsync(int id, IFormFile? profileImage)
    {
        var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
        if (mentor == null)
            return new Response<string>(HttpStatusCode.NotFound, "Устод ёфт нашуд");

        var imageResult = await FileUploadHelper.UploadFileAsync(
            profileImage, uploadPath, "mentor", "profile", true, mentor.ProfileImage);
        if (imageResult.StatusCode != (int)HttpStatusCode.OK)
            return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);

        mentor.ProfileImage = imageResult.Data;
        mentor.UpdatedAt = DateTimeOffset.UtcNow;

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
            ? new Response<string>(HttpStatusCode.OK, "Акси профил бо муваффақият навсозӣ шуд")
            : new Response<string>(HttpStatusCode.BadRequest, "Хатогӣ ҳангоми навсозии акси профил");
    }

    public async Task<Response<string>> UpdateMentorPaymentStatusAsync(int mentorId, PaymentStatus status)
    {
        var mentorsQuery = context.Mentors.Where(m => m.Id == mentorId && !m.IsDeleted);
        mentorsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(mentorsQuery, httpContextAccessor, m => m.CenterId);
        var mentor = await mentorsQuery.FirstOrDefaultAsync();
        if (mentor == null)
            return new Response<string>(HttpStatusCode.NotFound, "Устод ёфт нашуд");

        if (mentor.PaymentStatus == status)
        {
            return new Response<string>(HttpStatusCode.OK, "Ҳолати пардохт аллакай чунин аст");
        }

        mentor.PaymentStatus = status;
        mentor.UpdatedAt = DateTimeOffset.UtcNow;

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
            ? new Response<string>(HttpStatusCode.OK, "Ҳолати пардохт бо муваффақият навсозӣ шуд")
            : new Response<string>(HttpStatusCode.BadRequest, "Хатогӣ ҳангоми навсозии ҳолати пардохт");
    }

    public async Task<Response<List<GetSimpleDto>>> GetSimpleMentorPagination()
    {
        try
        {
            var query = context.Mentors.Where(m => !m.IsDeleted);
            query = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(query, httpContextAccessor, m => m.CenterId);
            var mentors = await query.OrderBy(m => m.FullName).Select(m => new GetSimpleDto()
            {
                Id = m.Id,
                FullName = m.FullName
            }).ToListAsync();
            if (mentors.Count > 0)
            {
                return new Response<List<GetSimpleDto>>(mentors);
            }
            else
            {
                
                return new Response<List<GetSimpleDto>>(HttpStatusCode.NotFound,"Mentors not found");
            }
        }
        catch (Exception ex)
        {
            return new Response<List<GetSimpleDto>>(HttpStatusCode.InternalServerError
            ,"Something went wrong");
        }
    }
}