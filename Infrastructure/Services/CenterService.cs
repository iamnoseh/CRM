using System.Net;
using Domain.DTOs.Center;
using Domain.DTOs.Course;
using Domain.DTOs.Group;
using Domain.DTOs.Mentor;
using Domain.DTOs.Student;
using Domain.Entities;
using Domain.Enums;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Constants;
using Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class CenterService(DataContext context, string uploadPath, IHttpContextAccessor httpContextAccessor) : ICenterService
{
    private readonly string[] _allowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif"];
    private const long MaxImageSize = 50 * 1024 * 1024;

    #region CreateCenterAsync

    public async Task<Response<string>> CreateCenterAsync(CreateCenterDto createCenterDto)
    {
        try
        {
            string imagePath = string.Empty;
            if (createCenterDto.ImageFile is { Length: > 0 })
            {
                var fileExtension = Path.GetExtension(createCenterDto.ImageFile.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(fileExtension))
                    return new Response<string>(HttpStatusCode.BadRequest, Messages.File.InvalidFileFormat);

                if (createCenterDto.ImageFile.Length > MaxImageSize)
                    return new Response<string>(HttpStatusCode.BadRequest, Messages.File.FileTooLarge);

                var uploadsFolder = Path.Combine(uploadPath, "uploads", "centers");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await createCenterDto.ImageFile.CopyToAsync(fileStream);
                }

                imagePath = $"/uploads/centers/{uniqueFileName}";
            }

            var center = new Center
            {
                Name = createCenterDto.Name,
                Description = createCenterDto.Description ?? string.Empty,
                Address = createCenterDto.Address,
                ContactPhone = createCenterDto.ContactPhone,
                Email = createCenterDto.ContactEmail,
                ManagerId = null,
                Image = imagePath,
                MonthlyIncome = 0,
                YearlyIncome = 0,
                StudentCapacity = createCenterDto.StudentCapacity,
                IsActive = createCenterDto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.Centers.AddAsync(center);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.Created, Messages.Center.Created)
                : new Response<string>(HttpStatusCode.BadRequest, Messages.Center.CreationError);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Center.CreationError, ex.Message));
        }
    }

    #endregion

    #region UpdateCenterAsync

    public async Task<Response<string>> UpdateCenterAsync(int id, UpdateCenterDto updateCenterDto)
    {
        try
        {
            var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (center == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            string imagePath = center.Image;

            if (updateCenterDto.ImageFile != null && updateCenterDto.ImageFile.Length > 0)
            {
                var fileExtension = Path.GetExtension(updateCenterDto.ImageFile.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(fileExtension))
                    return new Response<string>(HttpStatusCode.BadRequest, Messages.File.InvalidFileFormat);

                if (updateCenterDto.ImageFile.Length > MaxImageSize)
                    return new Response<string>(HttpStatusCode.BadRequest, Messages.File.FileTooLarge);

                if (!string.IsNullOrEmpty(center.Image))
                {
                    var oldImagePath = Path.Combine(uploadPath, center.Image.TrimStart('/'));
                    if (File.Exists(oldImagePath))
                        File.Delete(oldImagePath);
                }

                var uploadsFolder = Path.Combine(uploadPath, "uploads", "centers");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await updateCenterDto.ImageFile.CopyToAsync(fileStream);
                }

                imagePath = $"/uploads/centers/{uniqueFileName}";
            }

            if (updateCenterDto.ManagerId.HasValue)
            {
                var manager = await context.Users.FirstOrDefaultAsync(u => u.Id == updateCenterDto.ManagerId.Value && !u.IsDeleted);
                if (manager == null)
                    return new Response<string>(HttpStatusCode.BadRequest, Messages.User.NotFound);
            }

            center.Name = updateCenterDto.Name;
            center.Description = updateCenterDto.Description ?? string.Empty;
            center.Address = updateCenterDto.Address;
            center.ContactPhone = updateCenterDto.ContactPhone;
            center.Email = updateCenterDto.ContactEmail;
            center.ManagerId = updateCenterDto.ManagerId;
            center.Image = imagePath;
            center.StudentCapacity = updateCenterDto.StudentCapacity;
            center.IsActive = updateCenterDto.IsActive;
            center.UpdatedAt = DateTime.UtcNow;

            context.Centers.Update(center);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, Messages.Center.Updated)
                : new Response<string>(HttpStatusCode.BadRequest, Messages.Center.UpdateError);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Center.UpdateError, ex.Message));
        }
    }

    #endregion

    #region DeleteCenterAsync

    public async Task<Response<string>> DeleteCenterAsync(int id)
    {
        try
        {
            var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (center == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            var hasStudents = await context.Students.AnyAsync(s => s.CenterId == id && !s.IsDeleted);
            var hasMentors = await context.Mentors.AnyAsync(m => m.CenterId == id && !m.IsDeleted);
            var hasCourses = await context.Courses.AnyAsync(c => c.CenterId == id && !c.IsDeleted);

            if (hasStudents || hasMentors || hasCourses)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Center.CannotDeleteWithDependencies);

            center.IsDeleted = true;
            center.UpdatedAt = DateTime.UtcNow;

            context.Centers.Update(center);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, Messages.Center.Deleted)
                : new Response<string>(HttpStatusCode.BadRequest, Messages.Center.DeleteError);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Center.DeleteError, ex.Message));
        }
    }

    #endregion

    #region GetCenters

    public async Task<Response<List<GetCenterDto>>> GetCenters()
    {
        try
        {
            var centers = await context.Centers
                .Where(c => !c.IsDeleted)
                .Include(c => c.Manager)
                .ToListAsync();

            if (!centers.Any())
                return new Response<List<GetCenterDto>>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            var centerDtos = centers.Select(c => DtoMappingHelper.MapToGetCenterDto(c, context)).ToList();
            return new Response<List<GetCenterDto>>(centerDtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GetCenterDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetCenterByIdAsync

    public async Task<Response<GetCenterDto>> GetCenterByIdAsync(int id)
    {
        try
        {
            var center = await context.Centers
                .Where(c => c.Id == id && !c.IsDeleted)
                .Include(c => c.Manager)
                .FirstOrDefaultAsync();

            if (center == null)
                return new Response<GetCenterDto>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            var centerDto = DtoMappingHelper.MapToGetCenterDto(center, context);
            return new Response<GetCenterDto>(centerDto);
        }
        catch (Exception ex)
        {
            return new Response<GetCenterDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetCentersPaginated

    public async Task<PaginationResponse<List<GetCenterDto>>> GetCentersPaginated(CenterFilter filter)
    {
        var query = context.Centers.Where(c => !c.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(c => c.Name.ToLower().Contains(filter.Name.ToLower()));

        if (filter.IsActive.HasValue)
            query = query.Where(c => c.IsActive == filter.IsActive.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(c => c.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(c => c.CreatedAt <= filter.ToDate.Value);

        var totalRecords = await query.CountAsync();
        var skip = (filter.PageNumber - 1) * filter.PageSize;

        var centers = await query
            .OrderByDescending(c => c.CreatedAt)
            .Include(c => c.Manager)
            .Skip(skip)
            .Take(filter.PageSize)
            .ToListAsync();

        var centerDtos = centers.Select(c => DtoMappingHelper.MapToGetCenterDto(c, context)).ToList();
        return new PaginationResponse<List<GetCenterDto>>(centerDtos, totalRecords, filter.PageNumber, filter.PageSize);
    }

    #endregion

    #region GetCentersSimplePaginated

    public async Task<PaginationResponse<List<GetCenterSimpleDto>>> GetCentersSimplePaginated(int page, int pageSize)
    {
        try
        {
            var query = context.Centers.Where(c => !c.IsDeleted).AsQueryable();
            var totalRecords = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var centers = await query
                .OrderBy(c => c.Name)
                .Skip(skip)
                .Take(pageSize)
                .Select(c => DtoMappingHelper.MapToGetCenterSimpleDto(c))
                .ToListAsync();

            return new PaginationResponse<List<GetCenterSimpleDto>>(centers, totalRecords, page, pageSize);
        }
        catch
        {
            return new PaginationResponse<List<GetCenterSimpleDto>>(new List<GetCenterSimpleDto>(), 0, page, pageSize);
        }
    }

    #endregion

    #region GetCenterGroupsAsync

    public async Task<Response<List<GetCenterGroupsDto>>> GetCenterGroupsAsync(int centerId)
    {
        if (!HasAccessToCenter(centerId))
            return new Response<List<GetCenterGroupsDto>>(HttpStatusCode.Forbidden, Messages.Common.AccessDenied);

        try
        {
            var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == centerId && !c.IsDeleted);
            if (center == null)
                return new Response<List<GetCenterGroupsDto>>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            var groups = await context.Groups
                .Where(g => g.Course!.CenterId == centerId && !g.IsDeleted)
                .Include(g => g.Course)
                .ToListAsync();

            var result = new List<GetCenterGroupsDto>
            {
                new()
                {
                    CenterId = centerId,
                    CenterName = center.Name,
                    Groups = groups.Select(g => new GetGroupDto
                    {
                        Id = g.Id,
                        Name = g.Name,
                        Description = g.Description,
                        StartDate = g.StartDate,
                        EndDate = g.EndDate,
                        Status = g.Status,
                        CurrentWeek = g.CurrentWeek,
                        Started = g.Started,
                        CurrentStudentsCount = context.StudentGroups.Count(sg => sg.GroupId == g.Id && !sg.IsDeleted),
                    }).ToList(),
                    TotalGroups = groups.Count,
                    ActiveGroups = groups.Count(g => g.Status == ActiveStatus.Active)
                }
            };

            return new Response<List<GetCenterGroupsDto>>(result);
        }
        catch (Exception ex)
        {
            return new Response<List<GetCenterGroupsDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetCenterStudentsAsync

    public async Task<Response<List<GetCenterStudentsDto>>> GetCenterStudentsAsync(int centerId)
    {
        if (!HasAccessToCenter(centerId))
            return new Response<List<GetCenterStudentsDto>>(HttpStatusCode.Forbidden, Messages.Common.AccessDenied);

        try
        {
            var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == centerId && !c.IsDeleted);
            if (center == null)
                return new Response<List<GetCenterStudentsDto>>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            var students = await context.Students
                .Where(s => s.CenterId == centerId && !s.IsDeleted)
                .ToListAsync();

            var result = new List<GetCenterStudentsDto>
            {
                new()
                {
                    CenterId = centerId,
                    CenterName = center.Name,
                    Students = students.Select(s => new GetStudentDto
                    {
                        Id = s.Id,
                        FullName = s.FullName,
                        Email = s.Email,
                        Phone = s.PhoneNumber,
                        Address = s.Address,
                        Birthday = s.Birthday,
                        Age = s.Age,
                        Gender = s.Gender,
                        ActiveStatus = s.ActiveStatus,
                        PaymentStatus = s.PaymentStatus,
                        ImagePath = context.Users.Where(u => u.Id == s.UserId).Select(u => u.ProfileImagePath).FirstOrDefault() ?? s.ProfileImage,
                        UserId = s.UserId,
                        CenterId = s.CenterId
                    }).ToList(),
                    TotalStudents = students.Count,
                    ActiveStudents = students.Count(s => s.ActiveStatus == ActiveStatus.Active)
                }
            };

            return new Response<List<GetCenterStudentsDto>>(result);
        }
        catch (Exception ex)
        {
            return new Response<List<GetCenterStudentsDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetCenterMentorsAsync

    public async Task<Response<List<GetCenterMentorsDto>>> GetCenterMentorsAsync(int centerId)
    {
        if (!HasAccessToCenter(centerId))
            return new Response<List<GetCenterMentorsDto>>(HttpStatusCode.Forbidden, Messages.Common.AccessDenied);

        try
        {
            var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == centerId && !c.IsDeleted);
            if (center == null)
                return new Response<List<GetCenterMentorsDto>>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            var mentors = await context.Mentors
                .Where(m => m.CenterId == centerId && !m.IsDeleted)
                .ToListAsync();

            var result = new List<GetCenterMentorsDto>
            {
                new()
                {
                    CenterId = centerId,
                    CenterName = center.Name,
                    Mentors = mentors.Select(m => new GetMentorDto
                    {
                        Id = m.Id,
                        FullName = m.FullName,
                        Email = m.Email,
                        Phone = m.PhoneNumber,
                        Address = m.Address,
                        ImagePath = context.Users.Where(u => u.Id == m.UserId).Select(u => u.ProfileImagePath).FirstOrDefault() ?? m.ProfileImage,
                        Gender = m.Gender,
                        Birthday = m.Birthday,
                        Age = m.Age,
                        ActiveStatus = m.ActiveStatus,
                        UserId = m.UserId,
                        CenterId = m.CenterId
                    }).ToList(),
                    TotalMentors = mentors.Count,
                    ActiveMentors = mentors.Count(m => m.ActiveStatus == ActiveStatus.Active)
                }
            };

            return new Response<List<GetCenterMentorsDto>>(result);
        }
        catch (Exception ex)
        {
            return new Response<List<GetCenterMentorsDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetCenterCoursesAsync

    public async Task<Response<List<GetCenterCoursesDto>>> GetCenterCoursesAsync(int centerId)
    {
        if (!HasAccessToCenter(centerId))
            return new Response<List<GetCenterCoursesDto>>(HttpStatusCode.Forbidden, Messages.Common.AccessDenied);

        try
        {
            var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == centerId && !c.IsDeleted);
            if (center == null)
                return new Response<List<GetCenterCoursesDto>>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            var courses = await context.Courses
                .Where(c => c.CenterId == centerId && !c.IsDeleted)
                .ToListAsync();

            var result = new List<GetCenterCoursesDto>
            {
                new()
                {
                    CenterId = centerId,
                    CenterName = center.Name,
                    Courses = courses.Select(c => DtoMappingHelper.MapToGetCourseDto(c)).ToList(),
                    TotalCourses = courses.Count,
                    ActiveCourses = courses.Count(c => c.Status == ActiveStatus.Active)
                }
            };

            return new Response<List<GetCenterCoursesDto>>(result);
        }
        catch (Exception ex)
        {
            return new Response<List<GetCenterCoursesDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetCenterCoursesWithStatsAsync

    public async Task<Response<List<GetCourseWithStatsDto>>> GetCenterCoursesWithStatsAsync(int centerId)
    {
        if (!HasAccessToCenter(centerId))
            return new Response<List<GetCourseWithStatsDto>>(HttpStatusCode.Forbidden, Messages.Common.AccessDenied);

        var courses = await context.Courses
            .Where(c => c.CenterId == centerId && !c.IsDeleted)
            .Select(c => new GetCourseWithStatsDto
            {
                Id = c.Id,
                CourseName = c.CourseName,
                Image = c.ImagePath,
                Price = c.Price,
                GroupCount = context.Groups.Count(g => g.CourseId == c.Id && !g.IsDeleted),
                StudentCount = context.Groups
                    .Where(g => g.CourseId == c.Id && !g.IsDeleted)
                    .SelectMany(g => g.StudentGroups.Where(sg => !sg.IsDeleted && sg.IsActive))
                    .Select(sg => sg.StudentId)
                    .Distinct()
                    .Count()
            })
            .ToListAsync();

        return new Response<List<GetCourseWithStatsDto>>(courses);
    }

    #endregion

    #region GetCenterStatisticsAsync

    public async Task<Response<CenterStatisticsDto>> GetCenterStatisticsAsync(int centerId)
    {
        if (!HasAccessToCenter(centerId))
            return new Response<CenterStatisticsDto>(HttpStatusCode.Forbidden, Messages.Common.AccessDenied);

        try
        {
            var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == centerId && !c.IsDeleted);
            if (center == null)
                return new Response<CenterStatisticsDto>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            var result = new CenterStatisticsDto
            {
                CenterId = centerId,
                CenterName = center.Name,
                TotalStudents = await context.Students.CountAsync(s => s.CenterId == centerId && !s.IsDeleted),
                TotalMentors = await context.Mentors.CountAsync(m => m.CenterId == centerId && !m.IsDeleted),
                TotalCourses = await context.Courses.CountAsync(c => c.CenterId == centerId && !c.IsDeleted),
                ActiveStudents = await context.Students.CountAsync(s => s.CenterId == centerId && !s.IsDeleted && s.ActiveStatus == ActiveStatus.Active),
                ActiveMentors = await context.Mentors.CountAsync(m => m.CenterId == centerId && !m.IsDeleted && m.ActiveStatus == ActiveStatus.Active),
                ActiveCourses = await context.Courses.CountAsync(c => c.CenterId == centerId && !c.IsDeleted && c.Status == ActiveStatus.Active),
            };

            return new Response<CenterStatisticsDto>(result);
        }
        catch (Exception ex)
        {
            return new Response<CenterStatisticsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region CalculateCenterIncomeAsync

    public async Task<Response<string>> CalculateCenterIncomeAsync(int centerId)
    {
        if (!HasAccessToCenter(centerId))
            return new Response<string>(HttpStatusCode.Forbidden, Messages.Common.AccessDenied);

        try
        {
            var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == centerId && !c.IsDeleted);
            if (center == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;

            var monthlyIncome = await context.Payments
                .Where(p => p.CenterId == centerId &&
                            p.Month == currentMonth &&
                            p.Year == currentYear &&
                            p.Status == PaymentStatus.Paid &&
                            !p.IsDeleted)
                .SumAsync(p => p.Amount);

            var startDate = DateTime.UtcNow.AddMonths(-11);
            var startMonth = startDate.Month;
            var startYear = startDate.Year;

            var yearlyIncome = await context.Payments
                .Where(p => p.CenterId == centerId &&
                          p.Status == PaymentStatus.Paid &&
                          !p.IsDeleted &&
                          ((p.Year == startYear && p.Month >= startMonth) ||
                           (p.Year == currentYear && p.Month <= currentMonth) ||
                           (p.Year > startYear && p.Year < currentYear)))
                .SumAsync(p => p.Amount);

            center.MonthlyIncome = monthlyIncome;
            center.YearlyIncome = yearlyIncome;
            center.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.OK, string.Format(Messages.Center.IncomeUpdated, monthlyIncome, yearlyIncome));
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region CalculateAllCentersIncomeAsync

    public async Task<Response<string>> CalculateAllCentersIncomeAsync()
    {
        if (!IsSuperAdmin())
            return new Response<string>(HttpStatusCode.Forbidden, Messages.Center.SuperAdminOnly);

        try
        {
            var centers = await context.Centers.Where(c => !c.IsDeleted).ToListAsync();
            if (!centers.Any())
                return new Response<string>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            int successCount = 0;
            var errors = new List<string>();

            foreach (var center in centers)
            {
                var result = await CalculateCenterIncomeAsync(center.Id);
                if (result.StatusCode == (int)HttpStatusCode.OK)
                    successCount++;
                else
                    errors.Add(string.Format(Messages.Center.IncomeUpdateErrorFormat, center.Id, result.Message));
            }

            if (errors.Any())
                return new Response<string>(HttpStatusCode.PartialContent,
                    string.Format(Messages.Center.IncomeUpdatePartial, successCount, string.Join("; ", errors)));

            return new Response<string>(HttpStatusCode.OK, string.Format(Messages.Center.AllIncomesUpdated, successCount));
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region Private Methods

    private bool HasAccessToCenter(int centerId)
    {
        var userCenterId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
        return IsSuperAdmin() || userCenterId == centerId;
    }

    private bool IsSuperAdmin()
    {
        var roles = httpContextAccessor.HttpContext?.User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
        return roles != null && roles.Contains("SuperAdmin");
    }

    #endregion
}
