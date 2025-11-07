using System.Net;
using Domain.DTOs.Student;
using Domain.DTOs.Payments;
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
                    createStudentDto.ProfilePhoto, uploadPath, "profiles", "profile");
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
            // Do not mark user as Completed by default; leave as is or Pending
            
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
                var smsMessage = $"Салом, {user.FullName}!\nUsername: {username},\nPassword: {password}\nЛутфан, барои ворид шудан ба система ба ин суроға ташриф оред: {loginUrl}\nKavsar Academy";
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
                PaymentStatus = Domain.Enums.PaymentStatus.Pending
            };

            await context.Students.AddAsync(student);
            var res = await context.SaveChangesAsync();

            if (res <= 0)
                return new Response<string>(HttpStatusCode.BadRequest, "Student Creation Failed");

            // Auto-create wallet with 0 balance and send code via SMS
            var walletCode = await GenerateUniqueWalletCodeAsync();
            var account = new StudentAccount
            {
                StudentId = student.Id,
                AccountCode = walletCode,
                Balance = 0,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await context.StudentAccounts.AddAsync(account);
            await context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(student.PhoneNumber))
            {
                var sms = $"Салом, {student.FullName}!\nКоди ҳамёни шумо: {walletCode}.\nИн кодро ҳангоми пур кардани ҳисоб ҳатман ба админ пешниҳод кунед. Лутфан рамзро нигоҳ доред ва гум накунед.";
                await osonSmsService.SendSmsAsync(student.PhoneNumber, sms);
            }

            return new Response<string>(HttpStatusCode.Created, "Student Created Successfully. Wallet created and code sent via SMS.");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private async Task<string> GenerateUniqueWalletCodeAsync()
    {
        var rnd = new Random();
        while (true)
        {
            var code = rnd.Next(0, 999999).ToString("D6");
            var exists = await context.StudentAccounts.AnyAsync(a => a.AccountCode == code);
            if (!exists) return code;
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
                updateStudentDto.ProfilePhoto, uploadPath, "profiles", "profile");
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
            ImagePath = context.Users.Where(u => u.Id == student.UserId).Select(u => u.ProfileImagePath).FirstOrDefault() ?? student.ProfileImage,
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
        var studentsQuery = context.Students
            .Where(s => !s.IsDeleted)
            .Include(s => s.StudentGroups)
                .ThenInclude(sg => sg.Group)
            .AsQueryable();
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            studentsQuery, httpContextAccessor, s => s.CenterId);

        // var mentorId = UserContextHelper.GetCurrentUserMentorId(httpContextAccessor);
        // if (mentorId != null)
        // {
        //     studentsQuery = studentsQuery.Where(s => s.StudentGroups.Any(sg => sg.Group.MentorId == mentorId.Value));
        // }

        if (!string.IsNullOrEmpty(filter.FullName))
            studentsQuery = studentsQuery.Where(s => s.FullName.ToLower().Contains(filter.FullName.ToLower()));

        if (!string.IsNullOrEmpty(filter.Email))
            studentsQuery = studentsQuery.Where(s => s.Email.ToLower().Contains(filter.Email.ToLower()));

        if (!string.IsNullOrEmpty(filter.PhoneNumber))
            studentsQuery = studentsQuery.Where(s => s.PhoneNumber.ToLower().Contains(filter.PhoneNumber.ToLower()));

        if (filter.CenterId.HasValue)
            studentsQuery = studentsQuery.Where(s => s.CenterId == filter.CenterId.Value);

        var currentMentorId = UserContextHelper.GetCurrentUserMentorId(httpContextAccessor);

        if (filter.MentorId.HasValue)
        {
            studentsQuery = studentsQuery.Where(s => s.StudentGroups.Any(sg => sg.Group.MentorId == filter.MentorId.Value && !sg.IsDeleted));
        }
        else if (currentMentorId.HasValue)
        {
            studentsQuery = studentsQuery.Where(s => s.StudentGroups.Any(sg => sg.Group.MentorId == currentMentorId.Value && !sg.IsDeleted));
        }

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
                ImagePath = context.Users.Where(u => u.Id == s.UserId).Select(u => u.ProfileImagePath).FirstOrDefault() ?? s.ProfileImage,
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

        var uploadResult = await FileUploadHelper.UploadFileAsync(profileImage, uploadPath, "profiles", "profile");
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

            if (filter.MentorId.HasValue)
            {
                query = query.Where(s => s.StudentGroups.Any(sg => sg.Group.MentorId == filter.MentorId.Value && !sg.IsDeleted));
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

    public async Task<PaginationResponse<List<GetPaymentDto>>> GetStudentPaymentsAsync(int studentId, int? month, int? year, int pageNumber, int pageSize)
    {
        try
        {
            var studentsQuery = context.Students.Where(s => !s.IsDeleted && s.Id == studentId);
            studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(studentsQuery, httpContextAccessor, s => s.CenterId);
            var student = await studentsQuery.Select(s => new { s.Id, s.CenterId }).FirstOrDefaultAsync();
            if (student == null)
                return new PaginationResponse<List<GetPaymentDto>>(HttpStatusCode.NotFound, "Student not found");

            var payments = context.Payments.AsNoTracking().Where(p => !p.IsDeleted && p.StudentId == studentId);
            payments = payments.Where(p => p.CenterId == student.CenterId);
            if (month.HasValue)
                payments = payments.Where(p => p.Month == month.Value);
            if (year.HasValue)
                payments = payments.Where(p => p.Year == year.Value);

            var total = await payments.CountAsync();
            var list = await payments
                .OrderByDescending(p => p.PaymentDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new GetPaymentDto
                {
                    Id = p.Id,
                    StudentId = p.StudentId,
                    GroupId = p.GroupId,
                    ReceiptNumber = p.ReceiptNumber,
                    OriginalAmount = p.OriginalAmount,
                    DiscountAmount = p.DiscountAmount,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    TransactionId = p.TransactionId,
                    Description = p.Description,
                    Status = p.Status,
                    PaymentDate = p.PaymentDate,
                    CenterId = p.CenterId,
                    Month = p.Month,
                    Year = p.Year
                })
                .ToListAsync();

            return new PaginationResponse<List<GetPaymentDto>>(list, total, pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetPaymentDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<List<StudentGroupOverviewDto>>> GetStudentGroupsOverviewAsync(int studentId)
    {
        try
        {
            var studentsQuery = context.Students.Where(s => !s.IsDeleted && s.Id == studentId);
            studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(studentsQuery, httpContextAccessor, s => s.CenterId);
            var student = await studentsQuery.Select(s => new { s.Id }).FirstOrDefaultAsync();
            if (student == null)
                return new Response<List<StudentGroupOverviewDto>>(HttpStatusCode.Forbidden, "Дастрасӣ манъ аст ё донишҷӯ ёфт нашуд");

            var groupItems = await context.StudentGroups
                .Where(sg => !sg.IsDeleted && sg.StudentId == studentId && sg.IsActive)
                .Join(context.Groups.Where(g => !g.IsDeleted), sg => sg.GroupId, g => g.Id, (sg, g) => new { sg.GroupId, GroupName = g.Name })
                .Distinct()
                .ToListAsync();

            if (groupItems.Count == 0)
                return new Response<List<StudentGroupOverviewDto>>(new List<StudentGroupOverviewDto>());

            var groupIds = groupItems.Select(x => x.GroupId).ToList();

            var latestPaymentsByGroup = await context.Payments
                .Where(p => !p.IsDeleted && p.StudentId == studentId && p.GroupId != null && groupIds.Contains(p.GroupId.Value))
                .GroupBy(p => p.GroupId!.Value)
                .Select(g => g
                    .OrderByDescending(p => p.PaymentDate)
                    .ThenByDescending(p => p.CreatedAt)
                    .Select(p => new { GroupId = p.GroupId!.Value, p.Status, p.PaymentDate })
                    .FirstOrDefault())
                .ToListAsync();

            var paymentsDict = latestPaymentsByGroup
                .Where(x => x != null)
                .ToDictionary(x => x!.GroupId, x => x);

            var journalStats = await context.Journals
                .Where(j => !j.IsDeleted && groupIds.Contains(j.GroupId))
                .Join(context.JournalEntries.Where(e => !e.IsDeleted && e.StudentId == studentId),
                    j => j.Id,
                    e => e.JournalId,
                    (j, e) => new { j.GroupId, Entry = e })
                .GroupBy(x => x.GroupId)
                .Select(g => new
                {
                    GroupId = g.Key,
                    PresentCount = g.Count(x => x.Entry.AttendanceStatus == AttendanceStatus.Present),
                    LateCount = g.Count(x => x.Entry.AttendanceStatus == AttendanceStatus.Late),
                    AbsentCount = g.Count(x => x.Entry.AttendanceStatus == AttendanceStatus.Absent),
                    SumScore = g.Sum(x => (x.Entry.Grade ?? 0m) + (x.Entry.BonusPoints ?? 0m)),
                    ScoredCount = g.Count(x => x.Entry.Grade != null || x.Entry.BonusPoints != null)
                })
                .ToListAsync();

            var journalDict = journalStats.ToDictionary(x => x.GroupId, x => x);

            var result = new List<StudentGroupOverviewDto>();
            foreach (var g in groupItems)
            {
                paymentsDict.TryGetValue(g.GroupId, out var pay);
                var paymentStatus = pay?.Status;
                if (paymentStatus == PaymentStatus.Paid)
                    paymentStatus = PaymentStatus.Completed;

                journalDict.TryGetValue(g.GroupId, out var stat);
                var totalEntries = (stat?.PresentCount ?? 0) + (stat?.LateCount ?? 0) + (stat?.AbsentCount ?? 0);
                var attendanceRate = totalEntries > 0
                    ? Math.Round((decimal)(stat!.PresentCount) * 100m / totalEntries, 2)
                    : 0m;
                var averageScore = 0m;
                if (stat != null && stat.ScoredCount > 0)
                {
                    averageScore = Math.Round(stat.SumScore / stat.ScoredCount, 2);
                }

                result.Add(new StudentGroupOverviewDto
                {
                    GroupId = g.GroupId,
                    GroupName = g.GroupName,
                    PaymentStatus = paymentStatus,
                    LastPaymentDate = pay?.PaymentDate,
                    AverageScore = averageScore,
                    AttendanceRatePercent = attendanceRate,
                    PresentCount = stat?.PresentCount ?? 0,
                    LateCount = stat?.LateCount ?? 0,
                    AbsentCount = stat?.AbsentCount ?? 0
                });
            }

            return new Response<List<StudentGroupOverviewDto>>(result);
        }
        catch (Exception ex)
        {
            return new Response<List<StudentGroupOverviewDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
