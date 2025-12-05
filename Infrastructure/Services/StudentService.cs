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
using Infrastructure.Constants;
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
    IConfiguration configuration,
    IJournalService journalService) : IStudentService
{
    #region CreateStudentAsync

    public async Task<Response<string>> CreateStudentAsync(CreateStudentDto createStudentDto)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Group.CenterIdNotFound);

            string profileImagePath = string.Empty;
            if (createStudentDto.ProfilePhoto != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(
                    createStudentDto.ProfilePhoto, uploadPath, "profiles", "profile");
                if (imageResult.StatusCode != 200)
                    return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message!);
                profileImagePath = imageResult.Data;
            }

            string documentPath = string.Empty;
            if (createStudentDto.DocumentFile != null)
            {
                var docResult = await FileUploadHelper.UploadFileAsync(
                    createStudentDto.DocumentFile, uploadPath, "student", "document");
                if (docResult.StatusCode != 200)
                    return new Response<string>((HttpStatusCode)docResult.StatusCode, docResult.Message!);
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
                return new Response<string>((HttpStatusCode)userResult.StatusCode, userResult.Message!);

            var (user, password, username) = userResult.Data;

            if (!string.IsNullOrEmpty(createStudentDto.Email))
            {
                await EmailHelper.SendLoginDetailsEmailAsync(
                    emailService,
                    createStudentDto.Email,
                    username,
                    password,
                    "Student");
            }

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                var loginUrl = configuration["AppSettings:LoginUrl"];
                var smsMessage = string.Format(Messages.Sms.WelcomeStudent, user.FullName, username, password, loginUrl);
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
                ActiveStatus = ActiveStatus.Active,
                PaymentStatus = PaymentStatus.Pending
            };

            await context.Students.AddAsync(student);
            var res = await context.SaveChangesAsync();

            if (res <= 0)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Student.CreationFailed);

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
                var sms = string.Format(Messages.Sms.WalletCode, student.FullName, walletCode);
                await osonSmsService.SendSmsAsync(student.PhoneNumber, sms);
            }

            return new Response<string>(HttpStatusCode.Created, Messages.Student.WalletCreated);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region UpdateStudentAsync

    public async Task<Response<string>> UpdateStudentAsync(int id, UpdateStudentDto updateStudentDto)
    {
        var studentsQuery = context.Students.Where(s => s.Id == id && !s.IsDeleted);
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(studentsQuery, httpContextAccessor, s => s.CenterId);
        var student = await studentsQuery.FirstOrDefaultAsync();
        if (student == null)
            return new Response<string>(HttpStatusCode.Forbidden, Messages.Student.AccessDenied);

        string newProfileImagePath = student.ProfileImage!;
        if (updateStudentDto.ProfilePhoto != null)
        {
            if (!string.IsNullOrEmpty(student.ProfileImage))
                FileDeleteHelper.DeleteFile(student.ProfileImage, uploadPath);

            var imageResult = await FileUploadHelper.UploadFileAsync(
                updateStudentDto.ProfilePhoto, uploadPath, "profiles", "profile");
            if (imageResult.StatusCode != 200)
                return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message!);
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
            student.ActiveStatus = updateStudentDto.ActiveStatus;

        if (Enum.IsDefined(typeof(PaymentStatus), updateStudentDto.PaymentStatus))
            student.PaymentStatus = updateStudentDto.PaymentStatus;

        student.ProfileImage = newProfileImagePath;
        student.UpdatedAt = DateTime.UtcNow;

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
            ? new Response<string>(HttpStatusCode.OK, Messages.Student.Updated)
            : new Response<string>(HttpStatusCode.BadRequest, Messages.Student.UpdateFailed);
    }

    #endregion

    #region DeleteStudentAsync

    public async Task<Response<string>> DeleteStudentAsync(int id)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, Messages.Student.NotFound);

        if (!string.IsNullOrEmpty(student.ProfileImage))
            FileDeleteHelper.DeleteFile(student.ProfileImage, uploadPath);

        if (!string.IsNullOrEmpty(student.Document))
            FileDeleteHelper.DeleteFile(student.Document, uploadPath);

        student.IsDeleted = true;
        student.UpdatedAt = DateTime.UtcNow;

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
            ? new Response<string>(HttpStatusCode.OK, Messages.Student.Deleted)
            : new Response<string>(HttpStatusCode.BadRequest, Messages.Student.DeleteFailed);
    }

    #endregion

    #region GetStudentForSelect

    public async Task<PaginationResponse<List<GetStudentForSelectDto>>> GetStudentForSelect(StudentFilterForSelect filter)
    {
        var studentsQuery = context.Students.Where(s => !s.IsDeleted);
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(studentsQuery, httpContextAccessor, s => s.CenterId);

        if (!string.IsNullOrWhiteSpace(filter.FullName))
            studentsQuery = studentsQuery.Where(s => EF.Functions.ILike(s.FullName, $"%{filter.FullName}%"));

        var totalRecords = await studentsQuery.CountAsync();
        var skip = (filter.PageNumber - 1) * filter.PageSize;

        var students = await studentsQuery
            .OrderBy(s => s.Id)
            .Skip(skip)
            .Take(filter.PageSize)
            .Select(s => DtoMappingHelper.MapToGetStudentForSelectDto(s))
            .ToListAsync();

        if (students.Count == 0)
            return new PaginationResponse<List<GetStudentForSelectDto>>(HttpStatusCode.NotFound, Messages.Student.NoStudentsFound);

        return new PaginationResponse<List<GetStudentForSelectDto>>(students, totalRecords, filter.PageNumber, filter.PageSize);
    }

    #endregion

    #region GetStudentByIdAsync

    public async Task<Response<GetStudentDto>> GetStudentByIdAsync(int id)
    {
        var studentsQuery = context.Students.Where(s => !s.IsDeleted && s.Id == id);
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(studentsQuery, httpContextAccessor, s => s.CenterId);
        var student = await studentsQuery.FirstOrDefaultAsync();

        if (student == null)
            return new Response<GetStudentDto>(HttpStatusCode.NotFound, Messages.Student.NotFound);

        var userImagePath = await context.Users
            .Where(u => u.Id == student.UserId)
            .Select(u => u.ProfileImagePath)
            .FirstOrDefaultAsync();

        var dto = DtoMappingHelper.MapToGetStudentDto(student, userImagePath);
        return new Response<GetStudentDto>(dto);
    }

    #endregion

    #region UpdateStudentDocumentAsync

    public async Task<Response<string>> UpdateStudentDocumentAsync(int studentId, IFormFile? documentFile)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, Messages.Student.NotFound);

        if (documentFile == null)
            return new Response<string>(HttpStatusCode.BadRequest, Messages.Student.DocumentRequired);

        if (!string.IsNullOrEmpty(student.Document))
            FileDeleteHelper.DeleteFile(student.Document, uploadPath);

        var uploadResult = await FileUploadHelper.UploadFileAsync(documentFile, uploadPath, "student", "document");
        if (uploadResult.StatusCode != 200)
            return new Response<string>((HttpStatusCode)uploadResult.StatusCode, uploadResult.Message!);

        student.Document = uploadResult.Data;
        student.UpdatedAt = DateTime.UtcNow;
        context.Students.Update(student);
        var res = await context.SaveChangesAsync();

        return res > 0
            ? new Response<string>(HttpStatusCode.OK, Messages.Student.DocumentUpdated)
            : new Response<string>(HttpStatusCode.BadRequest, Messages.Student.DocumentUpdateFailed);
    }

    #endregion

    #region GetStudentsPagination

    public async Task<PaginationResponse<List<GetStudentDto>>> GetStudentsPagination(StudentFilter filter)
    {
        var studentsQuery = context.Students
            .Where(s => !s.IsDeleted)
            .Include(s => s.StudentGroups)
                .ThenInclude(sg => sg.Group)
            .AsQueryable();
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(studentsQuery, httpContextAccessor, s => s.CenterId);

        studentsQuery = ApplyStudentFilters(studentsQuery, filter);

        var currentMentorId = UserContextHelper.GetCurrentUserMentorId(httpContextAccessor);
        if (filter.MentorId.HasValue)
            studentsQuery = studentsQuery.Where(s => s.StudentGroups.Any(sg => sg.Group!.MentorId == filter.MentorId.Value && !sg.IsDeleted));
        else if (currentMentorId.HasValue)
            studentsQuery = studentsQuery.Where(s => s.StudentGroups.Any(sg => sg.Group!.MentorId == currentMentorId.Value && !sg.IsDeleted));

        var totalRecords = await studentsQuery.CountAsync();
        var skip = (filter.PageNumber - 1) * filter.PageSize;

        var students = await studentsQuery
            .OrderBy(s => s.Id)
            .Skip(skip)
            .Take(filter.PageSize)
            .ToListAsync();

        var userIds = students.Select(s => s.UserId).Distinct().ToList();
        var userImages = await context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.ProfileImagePath);

        var studentDtos = students.Select(s =>
        {
            var imagePath = userImages.TryGetValue(s.UserId, out var img) ? img : s.ProfileImage;
            return DtoMappingHelper.MapToGetStudentDto(s, imagePath);
        }).ToList();

        return new PaginationResponse<List<GetStudentDto>>(studentDtos, totalRecords, filter.PageNumber, filter.PageSize);
    }

    #endregion

    #region GetStudentDocument

    public async Task<Response<byte[]>> GetStudentDocument(int studentId)
    {
        var studentsQuery = context.Students.Where(s => !s.IsDeleted && s.Id == studentId);
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(studentsQuery, httpContextAccessor, s => s.CenterId);
        var student = await studentsQuery.FirstOrDefaultAsync();

        if (student == null)
            return new Response<byte[]>(HttpStatusCode.NotFound, Messages.Student.NotFound);

        return await FileUploadHelper.GetFileAsync(student.Document, uploadPath);
    }

    #endregion

    #region UpdateUserProfileImageAsync

    public async Task<Response<string>> UpdateUserProfileImageAsync(int studentId, IFormFile? profileImage)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, Messages.Student.NotFound);

        if (profileImage == null)
            return new Response<string>(HttpStatusCode.BadRequest, Messages.Student.ProfileImageRequired);

        if (!string.IsNullOrEmpty(student.ProfileImage))
            FileDeleteHelper.DeleteFile(student.ProfileImage, uploadPath);

        var uploadResult = await FileUploadHelper.UploadFileAsync(profileImage, uploadPath, "profiles", "profile");
        if (uploadResult.StatusCode != 200)
            return new Response<string>((HttpStatusCode)uploadResult.StatusCode, uploadResult.Message!);

        student.ProfileImage = uploadResult.Data;
        student.UpdatedAt = DateTime.UtcNow;

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
            ? new Response<string>(HttpStatusCode.OK, Messages.Student.ProfileImageUpdated)
            : new Response<string>(HttpStatusCode.BadRequest, Messages.Student.ProfileImageUpdateFailed);
    }

    #endregion

    #region UpdateStudentPaymentStatusAsync

    public async Task<Response<string>> UpdateStudentPaymentStatusAsync(UpdateStudentPaymentStatusDto dto)
    {
        var studentsQuery = context.Students.Where(s => !s.IsDeleted && s.Id == dto.StudentId);
        studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(studentsQuery, httpContextAccessor, s => s.CenterId);
        var student = await studentsQuery.FirstOrDefaultAsync();

        if (student == null)
            return new Response<string>(HttpStatusCode.NotFound, Messages.Student.NotFound);

        student.PaymentStatus = dto.Status;
        student.UpdatedAt = DateTime.UtcNow;
        context.Students.Update(student);

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
            ? new Response<string>(HttpStatusCode.OK, Messages.Student.PaymentStatusUpdated)
            : new Response<string>(HttpStatusCode.BadRequest, Messages.Student.PaymentStatusUpdateFailed);
    }

    #endregion

    #region GetSimpleStudents

    public async Task<PaginationResponse<List<GetSimpleDto>>> GetSimpleStudents(StudentFilter filter)
    {
        try
        {
            var query = context.Students.Where(s => !s.IsDeleted);
            query = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(query, httpContextAccessor, s => s.CenterId);
            query = ApplyStudentFiltersExtended(query, filter);

            var totalRecords = await query.CountAsync();
            var skip = (filter.PageNumber - 1) * filter.PageSize;

            var students = await query
                .OrderBy(s => s.FullName)
                .Skip(skip)
                .Take(filter.PageSize)
                .ToListAsync();

            var simpleDtos = students.Select(s => DtoMappingHelper.MapToGetSimpleDto(s.Id, s.FullName)).ToList();

            return new PaginationResponse<List<GetSimpleDto>>(simpleDtos, totalRecords, filter.PageNumber, filter.PageSize);
        }
        catch
        {
            return new PaginationResponse<List<GetSimpleDto>>(HttpStatusCode.InternalServerError, Messages.Common.InternalError);
        }
    }

    #endregion

    #region GetStudentPaymentsAsync

    public async Task<PaginationResponse<List<GetPaymentDto>>> GetStudentPaymentsAsync(int studentId, int? month, int? year, int pageNumber, int pageSize)
    {
        try
        {
            var studentsQuery = context.Students.Where(s => !s.IsDeleted && s.Id == studentId);
            studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(studentsQuery, httpContextAccessor, s => s.CenterId);
            var student = await studentsQuery.Select(s => new { s.Id, s.CenterId }).FirstOrDefaultAsync();
            if (student == null)
                return new PaginationResponse<List<GetPaymentDto>>(HttpStatusCode.NotFound, Messages.Student.NotFound);

            var payments = context.Payments.AsNoTracking().Where(p => !p.IsDeleted && p.StudentId == studentId && p.CenterId == student.CenterId);
            if (month.HasValue)
                payments = payments.Where(p => p.Month == month.Value);
            if (year.HasValue)
                payments = payments.Where(p => p.Year == year.Value);

            var total = await payments.CountAsync();
            var list = await payments
                .OrderByDescending(p => p.PaymentDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paymentDtos = list.Select(DtoMappingHelper.MapToGetPaymentDto).ToList();

            return new PaginationResponse<List<GetPaymentDto>>(paymentDtos, total, pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetPaymentDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetStudentGroupsOverviewAsync

    public async Task<Response<List<StudentGroupOverviewDto>>> GetStudentGroupsOverviewAsync(int studentId)
    {
        try
        {
            var studentsQuery = context.Students.Where(s => !s.IsDeleted && s.Id == studentId);
            studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(studentsQuery, httpContextAccessor, s => s.CenterId);
            var student = await studentsQuery.Select(s => new { s.Id }).FirstOrDefaultAsync();
            if (student == null)
                return new Response<List<StudentGroupOverviewDto>>(HttpStatusCode.Forbidden, Messages.Student.AccessDenied);

            var groupItems = await context.Groups
                .Where(g => !g.IsDeleted && g.StudentGroups.Any(sg => !sg.IsDeleted && sg.IsActive && sg.StudentId == studentId))
                .Select(g => new
                {
                    GroupId = g.Id,
                    GroupName = g.Name,
                    GroupImagePath = g.PhotoPath,
                    CourseName = g.Course != null ? g.Course.CourseName : null,
                    CourseImagePath = g.Course != null ? g.Course.ImagePath : null
                })
                .ToListAsync();

            if (groupItems.Count == 0)
                return new Response<List<StudentGroupOverviewDto>>(new List<StudentGroupOverviewDto>());

            var groupIds = groupItems.Select(x => x.GroupId).ToList();
            var weeklyAvgByGroup = await GetWeeklyAverageByGroupAsync(groupIds, studentId);
            var nowUtc = DateTime.UtcNow;
            var paidThisMonthSet = await GetPaidThisMonthSetAsync(studentId, groupIds, nowUtc);
            var paymentsDict = await GetLatestPaymentsByGroupAsync(studentId, groupIds);
            var journalDict = await GetJournalStatsByGroupAsync(groupIds, studentId, nowUtc);
            var priceByGroupId = await GetPriceByGroupIdAsync(groupIds);

            var result = new List<StudentGroupOverviewDto>();
            foreach (var g in groupItems)
            {
                paymentsDict.TryGetValue(g.GroupId, out var pay);
                var paymentStatus = await DeterminePaymentStatusAsync(studentId, g.GroupId, paidThisMonthSet, priceByGroupId);
                journalDict.TryGetValue(g.GroupId, out var stat);

                var totalEntries = (stat?.PresentCount ?? 0) + (stat?.LateCount ?? 0) + (stat?.AbsentCount ?? 0);
                var attendanceRate = totalEntries > 0 ? Math.Round(stat!.PresentCount * 100m / totalEntries, 2) : 0m;
                var averageScore = weeklyAvgByGroup.TryGetValue(g.GroupId, out var avgWeek) ? (decimal)Math.Round(avgWeek, 2) : 0m;

                result.Add(new StudentGroupOverviewDto
                {
                    GroupId = g.GroupId,
                    GroupName = g.GroupName,
                    GroupImagePath = g.GroupImagePath,
                    CourseName = g.CourseName,
                    CourseImagePath = g.CourseImagePath,
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

    #endregion

    #region Private Helper Methods

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

    private static IQueryable<Student> ApplyStudentFilters(IQueryable<Student> query, StudentFilter filter)
    {
        if (!string.IsNullOrEmpty(filter.FullName))
            query = query.Where(s => s.FullName.ToLower().Contains(filter.FullName.ToLower()));

        if (!string.IsNullOrEmpty(filter.Email))
            query = query.Where(s => s.Email.ToLower().Contains(filter.Email.ToLower()));

        if (!string.IsNullOrEmpty(filter.PhoneNumber))
            query = query.Where(s => s.PhoneNumber.ToLower().Contains(filter.PhoneNumber.ToLower()));

        if (filter.CenterId.HasValue)
            query = query.Where(s => s.CenterId == filter.CenterId.Value);

        if (filter.Active.HasValue)
            query = query.Where(s => s.ActiveStatus == filter.Active.Value);

        if (filter.PaymentStatus.HasValue)
            query = query.Where(s => s.PaymentStatus == filter.PaymentStatus.Value);

        return query;
    }

    private static IQueryable<Student> ApplyStudentFiltersExtended(IQueryable<Student> query, StudentFilter filter)
    {
        query = ApplyStudentFilters(query, filter);

        if (filter.Gender.HasValue)
            query = query.Where(s => s.Gender == filter.Gender.Value);

        if (filter.MinAge.HasValue)
            query = query.Where(s => s.Age >= filter.MinAge.Value);

        if (filter.MaxAge.HasValue)
            query = query.Where(s => s.Age <= filter.MaxAge.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(s => s.ActiveStatus == (filter.IsActive.Value ? ActiveStatus.Active : ActiveStatus.Inactive));

        if (filter.JoinedDateFrom.HasValue)
            query = query.Where(s => s.CreatedAt >= filter.JoinedDateFrom.Value);

        if (filter.JoinedDateTo.HasValue)
            query = query.Where(s => s.CreatedAt <= filter.JoinedDateTo.Value);

        if (filter.GroupId.HasValue)
            query = query.Where(s => s.StudentGroups.Any(sg => sg.GroupId == filter.GroupId.Value && !sg.IsDeleted));

        if (filter.MentorId.HasValue)
            query = query.Where(s => s.StudentGroups.Any(sg => sg.Group!.MentorId == filter.MentorId.Value && !sg.IsDeleted));

        if (filter.CourseId.HasValue)
            query = query.Where(s => s.StudentGroups.Any(sg => sg.Group!.CourseId == filter.CourseId.Value && !sg.IsDeleted));

        return query;
    }

    private async Task<Dictionary<int, double>> GetWeeklyAverageByGroupAsync(List<int> groupIds, int studentId)
    {
        var result = new Dictionary<int, double>();
        foreach (var gid in groupIds)
        {
            var wk = await journalService.GetGroupWeeklyTotalsAsync(gid);
            var avg = wk.Data.StudentAggregates.FirstOrDefault(a => a.StudentId == studentId)?.AveragePointsPerWeek;
            if (avg.HasValue) result[gid] = avg.Value;
        }
        return result;
    }

    private async Task<HashSet<int>> GetPaidThisMonthSetAsync(int studentId, List<int> groupIds, DateTime nowUtc)
    {
        var paidIds = await context.Payments
            .Where(p => !p.IsDeleted &&
                        p.StudentId == studentId &&
                        p.GroupId != null &&
                        groupIds.Contains(p.GroupId!.Value) &&
                        p.Year == nowUtc.Year &&
                        p.Month == nowUtc.Month &&
                        (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid))
            .Select(p => p.GroupId!.Value)
            .Distinct()
            .ToListAsync();
        return new HashSet<int>(paidIds);
    }

    private async Task<Dictionary<int, PaymentInfo>> GetLatestPaymentsByGroupAsync(int studentId, List<int> groupIds)
    {
        var payments = await context.Payments
            .Where(p => !p.IsDeleted && p.StudentId == studentId && p.GroupId != null && groupIds.Contains(p.GroupId.Value))
            .GroupBy(p => p.GroupId!.Value)
            .Select(g => g.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.CreatedAt)
                .Select(p => new PaymentInfo { GroupId = p.GroupId!.Value, Status = p.Status, PaymentDate = p.PaymentDate })
                .FirstOrDefault())
            .ToListAsync();

        return payments.Where(x => x != null).ToDictionary(x => x.GroupId, x => x);
    }

    private class PaymentInfo
    {
        public int GroupId { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    private async Task<Dictionary<int, JournalStat>> GetJournalStatsByGroupAsync(List<int> groupIds, int studentId, DateTime nowUtc)
    {
        var stats = await context.Journals
            .Where(j => !j.IsDeleted && groupIds.Contains(j.GroupId))
            .Join(context.JournalEntries.Where(e => !e.IsDeleted && e.StudentId == studentId && e.EntryDate <= nowUtc),
                j => j.Id,
                e => e.JournalId,
                (j, e) => new { j.GroupId, Entry = e })
            .GroupBy(x => x.GroupId)
            .Select(g => new JournalStat
            {
                GroupId = g.Key,
                PresentCount = g.Count(x => x.Entry.AttendanceStatus == AttendanceStatus.Present),
                LateCount = g.Count(x => x.Entry.AttendanceStatus == AttendanceStatus.Late),
                AbsentCount = g.Count(x => x.Entry.AttendanceStatus == AttendanceStatus.Absent)
            })
            .ToListAsync();

        return stats.ToDictionary(x => x.GroupId, x => x);
    }

    private async Task<Dictionary<int, decimal>> GetPriceByGroupIdAsync(List<int> groupIds)
    {
        return await context.Groups
            .Include(g => g.Course)
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Course != null ? g.Course.Price : 0m);
    }

    private async Task<PaymentStatus> DeterminePaymentStatusAsync(int studentId, int groupId, HashSet<int> paidThisMonthSet, Dictionary<int, decimal> priceByGroupId)
    {
        if (paidThisMonthSet.Contains(groupId))
            return PaymentStatus.Completed;

        var price = priceByGroupId.TryGetValue(groupId, out var p) ? p : 0m;
        var discount = await context.StudentGroupDiscounts
            .Where(x => x.StudentId == studentId && x.GroupId == groupId && !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => x.DiscountAmount)
            .FirstOrDefaultAsync();
        var applied = Math.Min(price, discount);
        var net = price - applied;
        return net <= 0 ? PaymentStatus.Completed : PaymentStatus.Pending;
    }

    private class JournalStat
    {
        public int GroupId { get; set; }
        public int PresentCount { get; set; }
        public int LateCount { get; set; }
        public int AbsentCount { get; set; }
    }

    #endregion
}
