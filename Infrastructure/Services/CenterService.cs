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
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CenterService(DataContext context, string uploadPath) : ICenterService
{
    private readonly string[] _allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
    private const long MaxImageSize = 50 * 1024 * 1024; // 50MB

    #region CreateCenterAsync
    public async Task<Response<string>> CreateCenterAsync(CreateCenterDto createCenterDto)
    {
        try
        {
            string imagePath = string.Empty;
            
            // Обработка изображения, если оно предоставлено
            if (createCenterDto.ImageFile != null && createCenterDto.ImageFile.Length > 0)
            {
                var fileExtension = Path.GetExtension(createCenterDto.ImageFile.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(fileExtension))
                    return new Response<string>(HttpStatusCode.BadRequest, 
                        "Invalid image format. Allowed formats: .jpg, .jpeg, .png, .gif");

                if (createCenterDto.ImageFile.Length > MaxImageSize)
                    return new Response<string>(HttpStatusCode.BadRequest, 
                        "Image size must be less than 10MB");

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

            // Создание нового центра
            var center = new Center
            {
                Name = createCenterDto.Name,
                Description = createCenterDto.Description ?? string.Empty,
                Address = createCenterDto.Address,
                ContactPhone = createCenterDto.ContactPhone,
                Email = createCenterDto.ContactEmail,
                ManagerName = createCenterDto.ManagerName,
                Image = imagePath,
                MonthlyIncome = createCenterDto.MonthlyIncome,
                YearlyIncome = createCenterDto.YearlyIncome,
                StudentCapacity = createCenterDto.StudentCapacity,
                IsActive = createCenterDto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.Centers.AddAsync(center);
            var result = await context.SaveChangesAsync();

            return result > 0 
                ? new Response<string>(HttpStatusCode.Created, "Center created successfully")
                : new Response<string>(HttpStatusCode.BadRequest, "Failed to create center");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
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
                return new Response<string>(HttpStatusCode.NotFound, "Center not found");

            string imagePath = center.Image;
            
            // Обработка изображения, если оно предоставлено
            if (updateCenterDto.ImageFile != null && updateCenterDto.ImageFile.Length > 0)
            {
                var fileExtension = Path.GetExtension(updateCenterDto.ImageFile.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(fileExtension))
                    return new Response<string>(HttpStatusCode.BadRequest, 
                        "Invalid image format. Allowed formats: .jpg, .jpeg, .png, .gif");

                if (updateCenterDto.ImageFile.Length > MaxImageSize)
                    return new Response<string>(HttpStatusCode.BadRequest, 
                        "Image size must be less than 10MB");

                // Удаление старого изображения, если оно существует
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

            // Обновление данных центра
            center.Name = updateCenterDto.Name;
            center.Description = updateCenterDto.Description ?? string.Empty;
            center.Address = updateCenterDto.Address;
            center.ContactPhone = updateCenterDto.ContactPhone;
            center.Email = updateCenterDto.ContactEmail;
            center.ManagerName = updateCenterDto.ManagerName;
            center.Image = imagePath;
            center.MonthlyIncome = updateCenterDto.MonthlyIncome;
            center.YearlyIncome = updateCenterDto.YearlyIncome;
            center.StudentCapacity = updateCenterDto.StudentCapacity;
            center.IsActive = updateCenterDto.IsActive;
            center.UpdatedAt = DateTime.UtcNow;

            context.Centers.Update(center);
            var result = await context.SaveChangesAsync();

            return result > 0 
                ? new Response<string>(HttpStatusCode.OK, "Center updated successfully")
                : new Response<string>(HttpStatusCode.BadRequest, "Failed to update center");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
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
                return new Response<string>(HttpStatusCode.NotFound, "Center not found");

            // Проверка на связанные сущности (опционально можно добавить дополнительные проверки)
            var hasStudents = await context.Students.AnyAsync(s => s.CenterId == id && !s.IsDeleted);
            var hasMentors = await context.Mentors.AnyAsync(m => m.CenterId == id && !m.IsDeleted);
            var hasCourses = await context.Courses.AnyAsync(c => c.CenterId == id && !c.IsDeleted);
            
            if (hasStudents || hasMentors || hasCourses)
                return new Response<string>(HttpStatusCode.BadRequest, 
                    "Cannot delete center with active students, mentors or courses");
            
            // Мягкое удаление (soft delete)
            center.IsDeleted = true;
            center.UpdatedAt = DateTime.UtcNow;
            
            context.Centers.Update(center);
            var result = await context.SaveChangesAsync();

            return result > 0 
                ? new Response<string>(HttpStatusCode.OK, "Center deleted successfully")
                : new Response<string>(HttpStatusCode.BadRequest, "Failed to delete center");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
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
                .Select(c => new GetCenterDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    Description = c.Description,
                    Image = c.Image,
                    MonthlyIncome = c.MonthlyIncome,
                    YearlyIncome = c.YearlyIncome,
                    StudentCapacity = c.StudentCapacity,
                    IsActive = c.IsActive,
                    ContactEmail = c.Email,
                    ContactPhone = c.ContactPhone,
                    ManagerName = c.ManagerName,
                    TotalStudents = context.Students.Count(s => s.CenterId == c.Id && !s.IsDeleted),
                    TotalMentors = context.Mentors.Count(m => m.CenterId == c.Id && !m.IsDeleted),
                    TotalCourses = context.Courses.Count(co => co.CenterId == c.Id && !co.IsDeleted),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return centers.Any()
                ? new Response<List<GetCenterDto>>(centers)
                : new Response<List<GetCenterDto>>(HttpStatusCode.NotFound, "No centers found");
        }
        catch (Exception ex)
        {
            return new Response<List<GetCenterDto>>(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
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
                .Select(c => new GetCenterDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    Description = c.Description,
                    Image = c.Image,
                    MonthlyIncome = c.MonthlyIncome,
                    YearlyIncome = c.YearlyIncome,
                    StudentCapacity = c.StudentCapacity,
                    IsActive = c.IsActive,
                    ContactEmail = c.Email,
                    ContactPhone = c.ContactPhone,
                    ManagerName = c.ManagerName,
                    TotalStudents = context.Students.Count(s => s.CenterId == c.Id && !s.IsDeleted),
                    TotalMentors = context.Mentors.Count(m => m.CenterId == c.Id && !m.IsDeleted),
                    TotalCourses = context.Courses.Count(co => co.CenterId == c.Id && !co.IsDeleted),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .FirstOrDefaultAsync();

            return center != null
                ? new Response<GetCenterDto>(center)
                : new Response<GetCenterDto>(HttpStatusCode.NotFound, "Center not found");
        }
        catch (Exception ex)
        {
            return new Response<GetCenterDto>(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
    #endregion

    #region GetCentersPaginated
    public async Task<PaginationResponse<List<GetCenterDto>>> GetCentersPaginated(CenterFilter filter)
    {

            var query = context.Centers.Where(c => !c.IsDeleted).AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(filter.Name))
                query = query.Where(c => c.Name.Contains(filter.Name));

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
                .Skip(skip)
                .Take(filter.PageSize)
                .Select(c => new GetCenterDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    Description = c.Description,
                    Image = c.Image,
                    MonthlyIncome = c.MonthlyIncome,
                    YearlyIncome = c.YearlyIncome,
                    StudentCapacity = c.StudentCapacity,
                    IsActive = c.IsActive,
                    ContactEmail = c.Email,
                    ContactPhone = c.ContactPhone,
                    ManagerName = c.ManagerName,
                    TotalStudents = context.Students.Count(s => s.CenterId == c.Id && !s.IsDeleted),
                    TotalMentors = context.Mentors.Count(m => m.CenterId == c.Id && !m.IsDeleted),
                    TotalCourses = context.Courses.Count(co => co.CenterId == c.Id && !co.IsDeleted),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return new PaginationResponse<List<GetCenterDto>>(
                centers,
                totalRecords,
                filter.PageNumber,
                filter.PageSize);

    }
    #endregion

    #region GetCenterGroupsAsync
    public async Task<Response<List<GetCenterGroupsDto>>> GetCenterGroupsAsync(int centerId)
    {
        try
        {
            var center = await context.Centers
                .FirstOrDefaultAsync(c => c.Id == centerId && !c.IsDeleted);

            if (center == null)
                return new Response<List<GetCenterGroupsDto>>(HttpStatusCode.NotFound, "Center not found");

            var groups = await context.Groups
                .Where(g => g.Course.CenterId == centerId && !g.IsDeleted)
                .Include(g => g.Course)
                .ToListAsync();

            var result = new List<GetCenterGroupsDto>
            {
                new()
                {
                    CenterId = centerId,
                    CenterName = center.Name,
                    Groups = groups.Select(g => new GetGroupDto()
                    {
                        Id = g.Id,
                        Name = g.Name,
                        Description = g.Description,
                        MentorId = g.Mentor.Id,
                        CourseId = g.CourseId,
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
            return new Response<List<GetCenterGroupsDto>>(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
    #endregion

    #region GetCenterStudentsAsync
    public async Task<Response<List<GetCenterStudentsDto>>> GetCenterStudentsAsync(int centerId)
    {
        try
        {
            var center = await context.Centers
                .FirstOrDefaultAsync(c => c.Id == centerId && !c.IsDeleted);

            if (center == null)
                return new Response<List<GetCenterStudentsDto>>(HttpStatusCode.NotFound, "Center not found");

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
                        ImagePath = s.ProfileImage
                    }).ToList(),
                    TotalStudents = students.Count,
                    ActiveStudents = students.Count(s => s.ActiveStatus == Domain.Enums.ActiveStatus.Active)
                }
            };

            return new Response<List<GetCenterStudentsDto>>(result);
        }
        catch (Exception ex)
        {
            return new Response<List<GetCenterStudentsDto>>(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
    #endregion

    #region GetCenterMentorsAsync
    public async Task<Response<List<GetCenterMentorsDto>>> GetCenterMentorsAsync(int centerId)
    {
        try
        {
            var center = await context.Centers
                .FirstOrDefaultAsync(c => c.Id == centerId && !c.IsDeleted);

            if (center == null)
                return new Response<List<GetCenterMentorsDto>>(HttpStatusCode.NotFound, "Center not found");

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
                        ImagePath = m.ProfileImage,
                        Gender = m.Gender,
                        Birthday = m.Birthday,
                        Age = m.Age,
                        ActiveStatus = m.ActiveStatus,
                        PaymentStatus = m.PaymentStatus,
                        Salary = m.Salary,
                    }).ToList(),
                    TotalMentors = mentors.Count,
                    ActiveMentors = mentors.Count(m => m.ActiveStatus == ActiveStatus.Active)
                }
            };

            return new Response<List<GetCenterMentorsDto>>(result);
        }
        catch (Exception ex)
        {
            return new Response<List<GetCenterMentorsDto>>(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
    #endregion

    #region GetCenterCoursesAsync
    public async Task<Response<List<GetCenterCoursesDto>>> GetCenterCoursesAsync(int centerId)
    {
        try
        {
            var center = await context.Centers
                .FirstOrDefaultAsync(c => c.Id == centerId && !c.IsDeleted);

            if (center == null)
                return new Response<List<GetCenterCoursesDto>>(HttpStatusCode.NotFound, "Center not found");

            var courses = await context.Courses
                .Where(c => c.CenterId == centerId && !c.IsDeleted)
                .ToListAsync();

            var result = new List<GetCenterCoursesDto>
            {
                new()
                {
                    CenterId = centerId,
                    CenterName = center.Name,
                    Courses = courses.Select(c => new GetCourseDto
                    {
                        Id = c.Id,
                        CourseName = c.CourseName,
                        Description = c.Description,
                        DurationInMonth = c.DurationInMonth,
                        ImagePath = c.ImagePath,
                        Price = c.Price,
                        Status = c.Status,
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        CenterId = c.CenterId,
                        CenterName = c.CourseName,
                       
                    }).ToList(),
                    TotalCourses = courses.Count,
                    ActiveCourses = courses.Count(c => c.Status == Domain.Enums.ActiveStatus.Active)
                }
            };

            return new Response<List<GetCenterCoursesDto>>(result);
        }
        catch (Exception ex)
        {
            return new Response<List<GetCenterCoursesDto>>(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
    #endregion

    #region GetCenterStatisticsAsync
    public async Task<Response<CenterStatisticsDto>> GetCenterStatisticsAsync(int centerId)
    {
        try
        {
            var center = await context.Centers
                .FirstOrDefaultAsync(c => c.Id == centerId && !c.IsDeleted);

            if (center == null)
                return new Response<CenterStatisticsDto>(HttpStatusCode.NotFound, "Center not found");

            var totalStudents = await context.Students.CountAsync(s => s.CenterId == centerId && !s.IsDeleted);
            var totalMentors = await context.Mentors.CountAsync(m => m.CenterId == centerId && !m.IsDeleted);
            var totalCourses = await context.Courses.CountAsync(c => c.CenterId == centerId && !c.IsDeleted);
            var activeStudents = await context.Students.CountAsync(s => s.CenterId == centerId && !s.IsDeleted && s.ActiveStatus == Domain.Enums.ActiveStatus.Active);
            var activeMentors = await context.Mentors.CountAsync(m => m.CenterId == centerId && !m.IsDeleted && m.ActiveStatus == Domain.Enums.ActiveStatus.Active);
            var activeCourses = await context.Courses.CountAsync(c => c.CenterId == centerId && !c.IsDeleted && c.Status == Domain.Enums.ActiveStatus.Active);

            var result = new CenterStatisticsDto
            {
                CenterId = centerId,
                CenterName = center.Name,
                TotalStudents = totalStudents,
                TotalMentors = totalMentors,
                TotalCourses = totalCourses,
                ActiveStudents = activeStudents,
                ActiveMentors = activeMentors,
                ActiveCourses = activeCourses,
            };

            return new Response<CenterStatisticsDto>(result);
        }
        catch (Exception ex)
        {
            return new Response<CenterStatisticsDto>(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
        }
    }
    #endregion
}