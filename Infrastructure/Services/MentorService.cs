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
using Infrastructure.Constants;
using Infrastructure.Services.EmailService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class MentorService(
    DataContext context,
    UserManager<User> userManager,
    string uploadPath,
    IEmailService emailService,
    IHttpContextAccessor httpContextAccessor,
    IOsonSmsService osonSmsService,
    IConfiguration configuration) : IMentorService
{
    #region CreateMentorAsync

    public async Task<Response<string>> CreateMentorAsync(CreateMentorDto createMentorDto)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Group.CenterIdNotFound);

            string profileImagePath = string.Empty;
            if (createMentorDto.ProfileImage != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(
                    createMentorDto.ProfileImage, uploadPath, "profiles", "profile");
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
                Roles.Mentor,
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
            if (user.PaymentStatus != PaymentStatus.Completed)
            {
                user.PaymentStatus = PaymentStatus.Completed;
                await userManager.UpdateAsync(user);
            }

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

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                var loginUrl = configuration["AppSettings:LoginUrl"];
                var smsMessage = string.Format(Messages.Sms.WelcomeMentor, user.FullName, username, password, loginUrl);
                await osonSmsService.SendSmsAsync(user.PhoneNumber, smsMessage);
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
                ProfileImage = profileImagePath,
                Document = documentPath,
                ActiveStatus = ActiveStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            await context.Mentors.AddAsync(mentor);
            var res = await context.SaveChangesAsync();

            return res > 0
                ? new Response<string>(HttpStatusCode.Created, Messages.Mentor.Created)
                : new Response<string>(HttpStatusCode.BadRequest, Messages.Mentor.CreationError);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Mentor.CreationError, ex.Message));
        }
    }

    #endregion

    #region UpdateMentorAsync

    public async Task<Response<string>> UpdateMentorAsync(int id, UpdateMentorDto updateMentorDto)
    {
        try
        {
            var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Mentor.NotFound);

            var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == updateMentorDto.CenterId && !c.IsDeleted);
            if (center == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            if (updateMentorDto.ProfileImage != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(
                    updateMentorDto.ProfileImage, uploadPath, "profiles", "profile", true, mentor.ProfileImage);
                if (imageResult.StatusCode != (int)HttpStatusCode.OK)
                    return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);
                mentor.ProfileImage = imageResult.Data;
                if (mentor.UserId != 0)
                {
                    var linkedUser = await userManager.FindByIdAsync(mentor.UserId.ToString());
                    if (linkedUser != null)
                    {
                        linkedUser.ProfileImagePath = mentor.ProfileImage;
                        await userManager.UpdateAsync(linkedUser);
                    }
                }
            }

            mentor.FullName = updateMentorDto.FullName;
            mentor.Email = updateMentorDto.Email;
            mentor.PhoneNumber = updateMentorDto.PhoneNumber;
            mentor.Address = updateMentorDto.Address;
            mentor.Birthday = updateMentorDto.Birthday;
            mentor.Age = DateUtils.CalculateAge(updateMentorDto.Birthday);
            mentor.Gender = updateMentorDto.Gender;
            mentor.Experience = updateMentorDto.Experience;
            mentor.ActiveStatus = updateMentorDto.ActiveStatus;
            mentor.UpdatedAt = DateTimeOffset.UtcNow;

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
                ? new Response<string>(HttpStatusCode.OK, Messages.Mentor.Updated)
                : new Response<string>(HttpStatusCode.InternalServerError, Messages.Mentor.UpdateError);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Mentor.UpdateError, ex.Message));
        }
    }

    #endregion

    #region DeleteMentorAsync

    public async Task<Response<string>> DeleteMentorAsync(int id)
    {
        try
        {
            var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Mentor.NotFound);

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
                ? new Response<string>(HttpStatusCode.OK, Messages.Mentor.Deleted)
                : new Response<string>(HttpStatusCode.InternalServerError, Messages.Mentor.DeleteError);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Mentor.DeleteError, ex.Message));
        }
    }

    #endregion

    #region GetMentorByIdAsync

    public async Task<Response<GetMentorDto>> GetMentorByIdAsync(int id)
    {
        var mentorsQuery = context.Mentors.Where(m => m.Id == id && !m.IsDeleted);
        mentorsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(mentorsQuery, httpContextAccessor, m => m.CenterId);
        var m = await mentorsQuery.FirstOrDefaultAsync();

        if (m == null)
            return new Response<GetMentorDto>(HttpStatusCode.NotFound, Messages.Mentor.NotFound);

        var userImagePath = await context.Users
            .Where(u => u.Id == m.UserId)
            .Select(u => u.ProfileImagePath)
            .FirstOrDefaultAsync();

        var dto = DtoMappingHelper.MapToGetMentorDto(m, userImagePath);
        return new Response<GetMentorDto>(dto);
    }

    #endregion

    #region UpdateMentorDocumentAsync

    public async Task<Response<string>> UpdateMentorDocumentAsync(int mentorId, IFormFile? documentFile)
    {
        var mentor = await context.Mentors.FirstOrDefaultAsync(s => s.Id == mentorId && !s.IsDeleted);
        if (mentor == null)
            return new Response<string>(HttpStatusCode.NotFound, Messages.Mentor.NotFound);

        var docResult = await FileUploadHelper.UploadFileAsync(
            documentFile, uploadPath, "mentor", "document", true, mentor.Document);
        if (docResult.StatusCode != (int)HttpStatusCode.OK)
            return new Response<string>((HttpStatusCode)docResult.StatusCode, docResult.Message);

        mentor.Document = docResult.Data;
        mentor.UpdatedAt = DateTimeOffset.UtcNow;
        context.Mentors.Update(mentor);
        var res = await context.SaveChangesAsync();

        return res > 0
            ? new Response<string>(HttpStatusCode.OK, Messages.Mentor.DocumentUpdated)
            : new Response<string>(HttpStatusCode.BadRequest, Messages.Mentor.DocumentUpdateFailed);
    }

    #endregion

    #region GetMentorDocument

    public async Task<Response<byte[]>> GetMentorDocument(int mentorId)
    {
        var mentor = await context.Mentors.FirstOrDefaultAsync(s => s.Id == mentorId && !s.IsDeleted);
        if (mentor == null)
            return new Response<byte[]>(HttpStatusCode.NotFound, Messages.Mentor.NotFound);

        return await FileUploadHelper.GetFileAsync(mentor.Document, uploadPath);
    }

    #endregion

    #region GetMentorsPagination

    public async Task<PaginationResponse<List<GetMentorDto>>> GetMentorsPagination(MentorFilter filter)
    {
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

        var mentorsQuery = context.Mentors.Where(m => !m.IsDeleted);
        mentorsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(mentorsQuery, httpContextAccessor, m => m.CenterId);

        mentorsQuery = ApplyMentorFilters(mentorsQuery, filter);

        var totalRecords = await mentorsQuery.CountAsync();
        var skip = (pageNumber - 1) * pageSize;

        var mentors = await mentorsQuery
            .OrderBy(m => m.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        var dtos = mentors.Select(m =>
        {
            var userImage = context.Users.Where(u => u.Id == m.UserId).Select(u => u.ProfileImagePath).FirstOrDefault();
            return DtoMappingHelper.MapToGetMentorDto(m, userImage);
        }).ToList();

        return new PaginationResponse<List<GetMentorDto>>(dtos, totalRecords, pageNumber, pageSize);
    }

    #endregion

    #region GetMentorsByGroupAsync

    public async Task<Response<List<GetMentorDto>>> GetMentorsByGroupAsync(int groupId)
    {
        try
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, Messages.Group.NotFound);

            var mentorIds = await context.MentorGroups
                .Where(mg => mg.GroupId == groupId && mg.IsActive == true && !mg.IsDeleted)
                .Select(mg => mg.MentorId)
                .ToListAsync();

            if (mentorIds.Count == 0)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, Messages.Mentor.NoMentorsFoundForGroup);

            var mentors = await GetMentorDtosWithRolesAsync(mentorIds);
            return new Response<List<GetMentorDto>>(mentors);
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
            var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);
            if (course == null)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, Messages.Course.NotFound);

            var groupIds = await context.Groups
                .Where(g => g.CourseId == courseId && !g.IsDeleted)
                .Select(g => g.Id)
                .ToListAsync();

            if (groupIds.Count == 0)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, Messages.Group.NoGroupsFoundForCourse);

            var mentorIds = await context.MentorGroups
                .Where(mg => groupIds.Contains(mg.GroupId) && mg.IsActive == true && !mg.IsDeleted)
                .Select(mg => mg.MentorId)
                .Distinct()
                .ToListAsync();

            if (mentorIds.Count == 0)
                return new Response<List<GetMentorDto>>(HttpStatusCode.NotFound, Messages.Mentor.NoMentorsFoundForCourse);

            var mentors = await GetMentorDtosWithRolesAsync(mentorIds);
            return new Response<List<GetMentorDto>>(mentors);
        }
        catch (Exception ex)
        {
            return new Response<List<GetMentorDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region UpdateUserProfileImageAsync

    public async Task<Response<string>> UpdateUserProfileImageAsync(int id, IFormFile? profileImage)
    {
        var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
        if (mentor == null)
            return new Response<string>(HttpStatusCode.NotFound, Messages.Mentor.NotFound);

        var imageResult = await FileUploadHelper.UploadFileAsync(
            profileImage, uploadPath, "profiles", "profile", true, mentor.ProfileImage);
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
            ? new Response<string>(HttpStatusCode.OK, Messages.User.ProfileImageUpdated)
            : new Response<string>(HttpStatusCode.BadRequest, Messages.Mentor.ProfileImageUpdateFailed);
    }

    #endregion
    
    #region GetSimpleMentorPagination

    public async Task<Response<List<GetSimpleDto>>> GetSimpleMentorPagination()
    {
        try
        {
            var query = context.Mentors.Where(m => !m.IsDeleted);
            query = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(query, httpContextAccessor, m => m.CenterId);

            var mentors = await query
                .OrderBy(m => m.FullName)
                .Select(m => DtoMappingHelper.MapToGetSimpleDto(m.Id, m.FullName))
                .ToListAsync();

            return mentors.Count > 0
                ? new Response<List<GetSimpleDto>>(mentors)
                : new Response<List<GetSimpleDto>>(HttpStatusCode.NotFound, Messages.Mentor.NoMentorsFound);
        }
        catch
        {
            return new Response<List<GetSimpleDto>>(HttpStatusCode.InternalServerError, Messages.Common.InternalError);
        }
    }

    #endregion

    #region Private Helper Methods

    private static IQueryable<Mentor> ApplyMentorFilters(IQueryable<Mentor> query, MentorFilter filter)
    {
        if (!string.IsNullOrEmpty(filter.FullName))
            query = query.Where(m => m.FullName.ToLower().Contains(filter.FullName.ToLower()));

        if (!string.IsNullOrEmpty(filter.PhoneNumber))
            query = query.Where(m => m.PhoneNumber.ToLower().Contains(filter.PhoneNumber.ToLower()));

        if (filter.CenterId.HasValue)
            query = query.Where(m => m.CenterId == filter.CenterId.Value);

        if (filter.Age.HasValue)
            query = query.Where(m => m.Age == filter.Age.Value);

        if (filter.Gender.HasValue)
            query = query.Where(m => m.Gender == filter.Gender.Value);
        
        return query;
    }

    private async Task<List<GetMentorDto>> GetMentorDtosWithRolesAsync(List<int> mentorIds)
    {
        var mentors = await context.Mentors
            .Where(u => mentorIds.Contains(u.Id) && !u.IsDeleted)
            .ToListAsync();

        var result = new List<GetMentorDto>();
        foreach (var m in mentors)
        {
            var userImage = await context.Users
                .Where(u => u.Id == m.UserId)
                .Select(u => u.ProfileImagePath)
                .FirstOrDefaultAsync();

            var dto = DtoMappingHelper.MapToGetMentorDto(m, userImage);

            if (m.UserId > 0)
            {
                var user = await userManager.FindByIdAsync(m.UserId.ToString());
                if (user != null)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    dto.Role = roles.FirstOrDefault() ?? "Mentor";
                }
            }

            result.Add(dto);
        }

        return result;
    }

    #endregion
}
