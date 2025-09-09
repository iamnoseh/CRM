using System.Net;
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
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class StudentService(
    DataContext context,
    IHttpContextAccessor httpContextAccessor,
    UserManager<User> userManager,
    string uploadPath,
    IEmailService emailService,
    IOsonSmsService osonSmsService,
    IConfiguration configuration) : IStudentService
{
    public async Task<Response<string>> CreateStudentAsync(CreateStudentDto createStudentDto)
    {
        try
        {
            
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<string>(HttpStatusCode.BadRequest, "CenterId not found in token");
            string profileImagePath = string.Empty;
            if (createStudentDto.ProfilePhoto != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(
                    createStudentDto.ProfilePhoto, uploadPath, "student", "profile");
                if (imageResult.StatusCode != 200)
                    return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);
                profileImagePath = imageResult.Data;
            }
            
            string documentPath = string.Empty;
            if (createStudentDto.DocumentFile != null)
            {
                var docResult = await FileUploadHelper.UploadFileAsync(
                    createStudentDto.DocumentFile, uploadPath, "student", "document");
                if (docResult.StatusCode != 200)
                    return new Response<string>((HttpStatusCode)docResult.StatusCode, docResult.Message);
                documentPath = docResult.Data;
            }
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
                dto => centerId.Value,
                _ => profileImagePath);
            if (userResult.StatusCode != 200)
                return new Response<string>((HttpStatusCode)userResult.StatusCode, userResult.Message);

            var (user, password, username) = userResult.Data;
            if (user.PaymentStatus != PaymentStatus.Completed)
            {
                user.PaymentStatus = PaymentStatus.Completed;
                await userManager.UpdateAsync(user);
            }
            
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

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                var loginUrl = configuration["AppSettings:LoginUrl"];
                var smsMessage = $"Салом, {user.FullName}!\nUsername: {username},\nPassword: {password}.\nЛутфан, барои ворид шудан ба система ба ин суроға ташриф оред: {loginUrl}\nKavsar Academy";
                await osonSmsService.SendSmsAsync(user.PhoneNumber, smsMessage);
            }

            var student = new Student
            {
                FullName = createStudentDto.FullName,
                Email = createStudentDto.Email,
                Address = createStudentDto.Address,
                PhoneNumber = createStudentDto.PhoneNumber,
                Birthday = createStudentDto.Birthday,
                Age = DateUtils.CalculateAge(createStudentDto.Birthday),
                Gender = createStudentDto.Gender,
                CenterId = centerId.Value,
                UserId = user.Id,
                ProfileImage = profileImagePath,
                Document = documentPath,
                ActiveStatus = Domain.Enums.ActiveStatus.Active,
                PaymentStatus = Domain.Enums.PaymentStatus.Completed
            };

            await context.Students.AddAsync(student);
            var res = await context.SaveChangesAsync();

            return res > 0
                ? new Response<string>(HttpStatusCode.Created, "Student Created Successfully. Login details sent via Email and/or SMS.")
                : new Response<string>(HttpStatusCode.BadRequest, "Student Creation Failed");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<string>> UpdateStudentAsync(int id, UpdateStudentDto updateStudentDto)
    {
        var studentsQuery = context.Students.Where(s => s.Id == id && !s.IsDeleted);
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(studentsQuery, httpContextAccessor, s => s.CenterId);
        var student = await studentsQuery.FirstOrDefaultAsync();
        if (student == null)
            return new Response<string>(HttpStatusCode.Forbidden, "Дастрасӣ манъ аст ё донишҷӯ ёфт нашуд");
        string newProfileImagePath = student.ProfileImage;
        if (updateStudentDto.ProfilePhoto != null)
        {
            if (!string.IsNullOrEmpty(student.ProfileImage))
            {
                FileDeleteHelper.DeleteFile(student.ProfileImage, uploadPath);
            }

            var imageResult = await FileUploadHelper.UploadFileAsync(
                updateStudentDto.ProfilePhoto, uploadPath, "student", "profile");
            if (imageResult.StatusCode != 200)
                return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);
            newProfileImagePath = imageResult.Data;
        }

        student.FullName = updateStudentDto.FullName;
        student.Email = updateStudentDto.Email;
        student.Address = updateStudentDto.Address;
        student.PhoneNumber = updateStudentDto.PhoneNumber;
        student.Birthday = updateStudentDto.Birthday;
        student.Age = DateUtils.CalculateAge(updateStudentDto.Birthday);
        student.Gender = updateStudentDto.Gender;

        if (Enum.IsDefined(typeof(ActiveStatus), updateStudentDto.ActiveStatus))
        {
            student.ActiveStatus = updateStudentDto.ActiveStatus;
        }

        if (Enum.IsDefined(typeof(PaymentStatus), updateStudentDto.PaymentStatus))
        {
            student.PaymentStatus = updateStudentDto.PaymentStatus;
        }

        student.ProfileImage = newProfileImagePath;
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
                    dto => dto.PaymentStatus,
                    _ => newProfileImagePath);
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

        if (!string.IsNullOrEmpty(student.ProfileImage))
        {
            FileDeleteHelper.DeleteFile(student.ProfileImage, uploadPath);
        }

        if (!string.IsNullOrEmpty(student.Document))
        {
            FileDeleteHelper.DeleteFile(student.Document, uploadPath);
        }

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
            .Where(s => !s.IsDeleted);
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            studentsQuery, httpContextAccessor, s => s.CenterId);
        var students = await studentsQuery
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
            })
            .ToListAsync();

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

    public async Task<PaginationResponse<List<GetStudentForSelectDto>>> GetStudentForSelect(
        StudentFilterForSelect filter)
    {
        var studentsQuery = context.Students.Where(s => !s.IsDeleted);
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            studentsQuery, httpContextAccessor, s => s.CenterId);

        if (!string.IsNullOrWhiteSpace(filter.FullName))
        {
            studentsQuery = studentsQuery
                .Where(s => EF.Functions.ILike(s.FullName, $"%{filter.FullName}%"));
        }

        var totalRecords = await studentsQuery.CountAsync();
        var skip = (filter.PageNumber - 1) * filter.PageSize;

        var students = await studentsQuery
            .OrderBy(s => s.Id)
            .Skip(skip)
            .Take(filter.PageSize)
            .Select(s => new GetStudentForSelectDto
            {
                Id = s.Id,
                FullName = s.FullName,
            })
            .ToListAsync();

        if (students.Count == 0)
        {
            return new PaginationResponse<List<GetStudentForSelectDto>>(HttpStatusCode.NotFound, "No students found");
        }

        return new PaginationResponse<List<GetStudentForSelectDto>>(
            students,
            totalRecords,
            filter.PageNumber,
            filter.PageSize
        );
    }


    public async Task<Response<GetStudentDto>> GetStudentByIdAsync(int id)
    {
        var studentsQuery = context.Students
            .Where(s => !s.IsDeleted && s.Id == id);
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            studentsQuery, httpContextAccessor, s => s.CenterId);
        var student = await studentsQuery.FirstOrDefaultAsync();

        if (student == null)
            return new Response<GetStudentDto>(HttpStatusCode.NotFound, "Student not found");

        var dto = new GetStudentDto
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
            Document = student.Document,
            UserId = student.UserId,
            CenterId = student.CenterId
        };

        return new Response<GetStudentDto>(dto);
    }

    public async Task<Response<string>> UpdateStudentDocumentAsync(int studentId, IFormFile? documentFile)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, "Student not found");

        if (documentFile == null)
            return new Response<string>(HttpStatusCode.BadRequest, "Document file is required");

        if (!string.IsNullOrEmpty(student.Document))
        {
            FileDeleteHelper.DeleteFile(student.Document, uploadPath);
        }

        var uploadResult = await FileUploadHelper.UploadFileAsync(documentFile, uploadPath, "student", "document");
        if (uploadResult.StatusCode != 200)
            return new Response<string>((HttpStatusCode)uploadResult.StatusCode, uploadResult.Message);

        student.Document = uploadResult.Data;
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
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            studentsQuery, httpContextAccessor, s => s.CenterId);

        if (!string.IsNullOrEmpty(filter.FullName))
            studentsQuery = studentsQuery.Where(s => s.FullName.ToLower().Contains(filter.FullName.ToLower()));

        if (!string.IsNullOrEmpty(filter.Email))
            studentsQuery = studentsQuery.Where(s => s.Email.ToLower().Contains(filter.Email.ToLower()));

        if (!string.IsNullOrEmpty(filter.PhoneNumber))
            studentsQuery = studentsQuery.Where(s => s.PhoneNumber.ToLower().Contains(filter.PhoneNumber.ToLower()));

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
                ImagePath = s.ProfileImage,
                UserId = s.UserId,
                CenterId = s.CenterId
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
        var studentsQuery = context.Students
            .Where(s => !s.IsDeleted && s.Id == studentId);
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            studentsQuery, httpContextAccessor, s => s.CenterId);
        var student = await studentsQuery.FirstOrDefaultAsync();

        if (student == null)
            return new Response<byte[]>(HttpStatusCode.NotFound, "Student not found");

        return await FileUploadHelper.GetFileAsync(student.Document, uploadPath);
    }

    public async Task<Response<string>> UpdateUserProfileImageAsync(int studentId, IFormFile? profileImage)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, "Student not found");

        if (profileImage == null)
            return new Response<string>(HttpStatusCode.BadRequest, "Profile image is required");

        if (!string.IsNullOrEmpty(student.ProfileImage))
        {
            FileDeleteHelper.DeleteFile(student.ProfileImage, uploadPath);
        }

        var uploadResult = await FileUploadHelper.UploadFileAsync(profileImage, uploadPath, "student", "profile");
        if (uploadResult.StatusCode != 200)
            return new Response<string>((HttpStatusCode)uploadResult.StatusCode, uploadResult.Message);

        student.ProfileImage = uploadResult.Data;
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


    public async Task<Response<string>> UpdateStudentPaymentStatusAsync(UpdateStudentPaymentStatusDto dto)
    {
        var studentsQuery = context.Students
            .Where(s => !s.IsDeleted && s.Id == dto.StudentId);
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            studentsQuery, httpContextAccessor, s => s.CenterId);
        var student = await studentsQuery.FirstOrDefaultAsync();

        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, "Student not found");

        student.PaymentStatus = dto.Status;
        student.UpdatedAt = DateTime.UtcNow;
        context.Students.Update(student);

        if (student.UserId != null)
        {
            var user = await userManager.FindByIdAsync(student.UserId.ToString());
            if (user != null)
            {
                user.PaymentStatus = dto.Status;
                await userManager.UpdateAsync(user);
            }
        }

        var res = await context.SaveChangesAsync();
        return res > 0
            ? new Response<string>(HttpStatusCode.OK, "Payment status updated successfully")
            : new Response<string>(HttpStatusCode.BadRequest, "Failed to update payment status");
    }

    public async Task<PaginationResponse<List<GetSimpleDto>>> GetSimpleStudents(StudentFilter filter)
    {
        try
        {
            var query = context.Students.Where(s => !s.IsDeleted);
            query = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(query, httpContextAccessor, s => s.CenterId);
            
            if (!string.IsNullOrEmpty(filter.FullName))
                query = query.Where(s => s.FullName.ToLower().Contains(filter.FullName.ToLower()));

            if (!string.IsNullOrEmpty(filter.PhoneNumber))
                query = query.Where(s => s.PhoneNumber.ToLower().Contains(filter.PhoneNumber.ToLower()));

            if (!string.IsNullOrEmpty(filter.Email))
                query = query.Where(s => s.Email.ToLower().Contains(filter.Email.ToLower()));

            if (filter.Active.HasValue)
                query = query.Where(s => s.ActiveStatus == filter.Active.Value);

            if (filter.PaymentStatus.HasValue)
                query = query.Where(s => s.PaymentStatus == filter.PaymentStatus.Value);

            if (filter.Gender.HasValue)
                query = query.Where(s => s.Gender == filter.Gender.Value);

            if (filter.MinAge.HasValue)
                query = query.Where(s => s.Age >= filter.MinAge.Value);

            if (filter.MaxAge.HasValue)
                query = query.Where(s => s.Age <= filter.MaxAge.Value);

            if (filter.CenterId.HasValue)
                query = query.Where(s => s.CenterId == filter.CenterId.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(s => s.ActiveStatus == (filter.IsActive.Value ? ActiveStatus.Active : ActiveStatus.Inactive));

            if (filter.JoinedDateFrom.HasValue)
                query = query.Where(s => s.CreatedAt >= filter.JoinedDateFrom.Value);

            if (filter.JoinedDateTo.HasValue)
                query = query.Where(s => s.CreatedAt <= filter.JoinedDateTo.Value);

            if (filter.GroupId.HasValue)
            {
                query = query.Where(s => s.StudentGroups.Any(sg => sg.GroupId == filter.GroupId.Value && !sg.IsDeleted));
            }

            if (filter.CourseId.HasValue)
            {
                query = query.Where(s => s.StudentGroups.Any(sg => sg.Group!.CourseId == filter.CourseId.Value && !sg.IsDeleted));
            }

            var totalRecords = await query.CountAsync();
            var skip = (filter.PageNumber - 1) * filter.PageSize;

            var students = await query
                .OrderBy(s => s.FullName)
                .Skip(skip)
                .Take(filter.PageSize)
                .Select(s => new GetSimpleDto
                {
                    Id = s.Id,
                    FullName = s.FullName
                })
                .ToListAsync();

            return new PaginationResponse<List<GetSimpleDto>>(
                students,
                totalRecords,
                filter.PageNumber,
                filter.PageSize);
        }
        catch 
        {
            return new PaginationResponse<List<GetSimpleDto>>(HttpStatusCode.InternalServerError,"Something went wrong");
        }
    }
}