using System.Net;
using Domain.DTOs.Course;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CourseService(DataContext context, string uploadPath,IHttpContextAccessor httpContextAccessor) : ICourseService
{
    private readonly string[] _allowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif"];
    private const long MaxImageSize = 50 * 1024 * 1024; 

    #region CreateCourseAsync
    public async Task<Response<string>> CreateCourseAsync(CreateCourseDto createCourseDto)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<string>(HttpStatusCode.BadRequest, "CenterId not found in token");

            if (createCourseDto.ImageFile != null)
            {
                var extension = Path.GetExtension(createCourseDto.ImageFile.FileName).ToLower();
                if (!_allowedImageExtensions.Contains(extension))
                    return new Response<string>(HttpStatusCode.BadRequest, "Invalid image format. Allowed formats: jpg, jpeg, png, gif");

                if (createCourseDto.ImageFile.Length > MaxImageSize)
                    return new Response<string>(HttpStatusCode.BadRequest, $"Image size exceeds the maximum allowed size of {MaxImageSize / (1024 * 1024)}MB");
            }

            var course = new Course
            {
                CourseName = createCourseDto.CourseName,
                Description = createCourseDto.Description,
                DurationInMonth = createCourseDto.DurationInMonth,
                Price = createCourseDto.Price,
                Status = createCourseDto.Status,
                CenterId = centerId.Value
            };

            if (createCourseDto.ImageFile != null)
            {
                var uploadsFolder = Path.Combine(uploadPath, "uploads", "courses");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(createCourseDto.ImageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using var stream = new FileStream(filePath, FileMode.Create);
                await createCourseDto.ImageFile.CopyToAsync(stream);
                
                course.ImagePath = $"/uploads/courses/{uniqueFileName}";
            }

            await context.Courses.AddAsync(course);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.Created, "Course created successfully")
                : new Response<string>(HttpStatusCode.BadRequest, "Failed to create course");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region UpdateCourseAsync
    public async Task<Response<string>> UpdateCourseAsync(UpdateCourseDto updateCourseDto)
    {
        try
        {
            var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == updateCourseDto.Id && !c.IsDeleted);
            if (course == null)
                return new Response<string>(HttpStatusCode.NotFound, "Course not found");

            // Проверяем существование центра
            if (course.CenterId != updateCourseDto.CenterId)
            {
                var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == updateCourseDto.CenterId && !c.IsDeleted);
                if (center == null)
                    return new Response<string>(HttpStatusCode.BadRequest, "Center not found");
            }

            // Проверка изображения, если оно обновляется
            if (updateCourseDto.ImageFile != null)
            {
                var extension = Path.GetExtension(updateCourseDto.ImageFile.FileName).ToLower();
                if (!_allowedImageExtensions.Contains(extension))
                    return new Response<string>(HttpStatusCode.BadRequest, "Invalid image format. Allowed formats: jpg, jpeg, png, gif");

                if (updateCourseDto.ImageFile.Length > MaxImageSize)
                    return new Response<string>(HttpStatusCode.BadRequest, $"Image size exceeds the maximum allowed size of {MaxImageSize / (1024 * 1024)}MB");

                // Удаление старого изображения
                if (!string.IsNullOrEmpty(course.ImagePath))
                {
                    var oldImagePath = Path.Combine(uploadPath, course.ImagePath.TrimStart('/'));
                    if (File.Exists(oldImagePath))
                        File.Delete(oldImagePath);
                }

                // Сохранение нового изображения
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(updateCourseDto.ImageFile.FileName);
                var filePath = Path.Combine(uploadPath, fileName);
                
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                
                using var stream = new FileStream(filePath, FileMode.Create);
                await updateCourseDto.ImageFile.CopyToAsync(stream);
                
                course.ImagePath = "/uploads/" + fileName;
            }

            // Обновление данных курса
            course.CourseName = updateCourseDto.CourseName;
            course.Description = updateCourseDto.Description;
            course.DurationInMonth = updateCourseDto.DurationInMonth;
            course.Price = updateCourseDto.Price;
            course.Status = updateCourseDto.Status;
            course.CenterId = updateCourseDto.CenterId;
            course.UpdatedAt = DateTime.UtcNow;

            context.Courses.Update(course);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Course updated successfully")
                : new Response<string>(HttpStatusCode.BadRequest, "Failed to update course");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region DeleteCourseAsync
    public async Task<Response<string>> DeleteCourseAsync(int id)
    {
        try
        {
            var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (course == null)
                return new Response<string>(HttpStatusCode.NotFound, "Course not found");

            course.IsDeleted = true;
            course.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(course.ImagePath))
            {
                var imagePath = Path.Combine(uploadPath, course.ImagePath.TrimStart('/'));
                if (File.Exists(imagePath))
                    File.Delete(imagePath);
            }

            context.Courses.Update(course);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Course deleted successfully")
                : new Response<string>(HttpStatusCode.BadRequest, "Failed to delete course");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetCourses
    public async Task<Response<List<GetCourseDto>>> GetCourses()
    {
        try
        {
            var coursesQuery = context.Courses
                .Include(c => c.Center)
                .Where(c => !c.IsDeleted);
            coursesQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
                coursesQuery, httpContextAccessor, c => c.CenterId);
            var courses = await coursesQuery
                .Select(c => new GetCourseDto
                {
                    Id = c.Id,
                    CourseName = c.CourseName,
                    Description = c.Description,
                    DurationInMonth = c.DurationInMonth,
                    Price = c.Price,
                    Status = c.Status,
                    ImagePath = c.ImagePath,
                    CenterId = c.CenterId,
                    CenterName = c.Center.Name
                })
                .ToListAsync();

             return courses.Any()
                ? new Response<List<GetCourseDto>>(courses)
                : new Response<List<GetCourseDto>>(System.Net.HttpStatusCode.NotFound, "No courses found");
        }
        catch (Exception ex)
        {
            return new Response<List<GetCourseDto>>(System.Net.HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetCourseByIdAsync
    public async Task<Response<GetCourseDto>> GetCourseByIdAsync(int id)
    {
        try
        {
            var coursesQuery = context.Courses
                .Include(c => c.Center)
                .Where(c => !c.IsDeleted && c.Id == id);
            coursesQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
                coursesQuery, httpContextAccessor, c => c.CenterId);
            var course = await coursesQuery.FirstOrDefaultAsync();

            if (course == null)
                return new Response<GetCourseDto>(System.Net.HttpStatusCode.NotFound, "Course not found");

            var courseDto = new GetCourseDto
            {
                Id = course.Id,
                CourseName = course.CourseName,
                Description = course.Description,
                DurationInMonth = course.DurationInMonth,
                Price = course.Price,
                Status = course.Status,
                ImagePath = course.ImagePath,
                CenterId = course.CenterId,
                CenterName = course.Center.Name
            };

            return new Response<GetCourseDto>(courseDto);
        }
        catch (Exception ex)
        {
            return new Response<GetCourseDto>(System.Net.HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetCoursesPagination
    public async Task<PaginationResponse<List<GetCourseDto>>> GetCoursesPagination(CourseFilter filter)
    {
        try
        {
            var coursesQuery = context.Courses.Where(c => !c.IsDeleted).AsQueryable();
            coursesQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
                coursesQuery, httpContextAccessor, c => c.CenterId);

            // Применение фильтров
            if (!string.IsNullOrEmpty(filter.Name))
            {
                coursesQuery = coursesQuery.Where(c => c.CourseName.ToLower().Contains(filter.Name.ToLower()));
            }

            if (filter.Price.HasValue)
                coursesQuery = coursesQuery.Where(c => c.Price == filter.Price.Value);

            if (filter.DurationInMonth.HasValue)
                coursesQuery = coursesQuery.Where(c => c.DurationInMonth == filter.DurationInMonth.Value);

            if (filter.Status.HasValue)
                coursesQuery = coursesQuery.Where(c => c.Status == filter.Status.Value);
            
            var totalRecords = await coursesQuery.CountAsync();

            var skip = (filter.PageNumber - 1) * filter.PageSize;
            var courses = await coursesQuery
                .OrderBy(c => c.Id)
                .Skip(skip)
                .Take(filter.PageSize)
                .Select(c => new GetCourseDto
                {
                    Id = c.Id,
                    CourseName = c.CourseName,
                    Description = c.Description,
                    DurationInMonth = c.DurationInMonth,
                    Price = c.Price,
                    Status = c.Status,
                    ImagePath = c.ImagePath,
                    CenterId = c.CenterId,
                    CenterName = c.Center.Name
                })
                .ToListAsync();

            return new PaginationResponse<List<GetCourseDto>>(
                courses, 
                totalRecords, 
                filter.PageNumber, 
                filter.PageSize);
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetCourseDto>>(System.Net.HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetCoursesByMentorAsync
    public async Task<Response<List<GetCourseDto>>> GetCoursesByMentorAsync(int mentorId)
    {
        try
        {
            var mentor = await context.Mentors
                .Include(m => m.Groups)
                .ThenInclude(g => g.Course)
                .FirstOrDefaultAsync(m => m.Id == mentorId && !m.IsDeleted);

            if (mentor == null)
                return new Response<List<GetCourseDto>>(System.Net.HttpStatusCode.NotFound, "Mentor not found");

            var courses = mentor.Groups
                .Where(g => !g.IsDeleted && g.Course != null && !g.Course.IsDeleted)
                .Select(g => g.Course)
                .Distinct()
                .AsQueryable();
            courses = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
                courses, httpContextAccessor, c => c.CenterId);
            var courseDtos = courses.Select(c => new GetCourseDto
            {
                Id = c.Id,
                CourseName = c.CourseName,
                Description = c.Description,
                DurationInMonth = c.DurationInMonth,
                Price = c.Price,
                Status = c.Status,
                ImagePath = c.ImagePath,
                CenterId = c.CenterId,
                CenterName = c.Center.Name
            }).ToList();

            return courseDtos.Any()
                ? new Response<List<GetCourseDto>>(courseDtos)
                : new Response<List<GetCourseDto>>(System.Net.HttpStatusCode.NotFound, "No courses found for this mentor");
        }
        catch (Exception ex)
        {
            return new Response<List<GetCourseDto>>(System.Net.HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetCourseGroupsAndCount
    public async Task<Response<GetCourseGroupsDto>> GetCourseGroupsAndCountAsync(int courseId)
    {
        try
        {
            var groupsQuery = context.Groups
                .Include(g => g.Course)
                .Where(g => g.CourseId == courseId && !g.IsDeleted);
            groupsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
                groupsQuery, httpContextAccessor, g => g.Course.CenterId);
            var groups = await groupsQuery.ToListAsync();
            var groupDtos = groups.Select(g => new Domain.DTOs.Group.GetGroupDto
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                CourseId = g.CourseId,
                DurationMonth = g.DurationMonth,
                LessonInWeek = g.LessonInWeek,
                TotalWeeks = g.TotalWeeks,
                Started = g.Started,
                Status = g.Status,
                MentorId = g.MentorId,
                ImagePath = g.PhotoPath,
                CurrentWeek = g.CurrentWeek,
                StartDate = g.StartDate,
                EndDate = g.EndDate
            }).ToList();
            var dto = new GetCourseGroupsDto
            {
                Groups = groupDtos,
                Count = groupDtos.Count
            };
            return new Response<GetCourseGroupsDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetCourseGroupsDto>(System.Net.HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion
}