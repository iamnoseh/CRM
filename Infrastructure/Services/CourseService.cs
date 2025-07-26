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

public class CourseService(DataContext context, string uploadPath, IHttpContextAccessor httpContextAccessor) : ICourseService
{
    public async Task<Response<string>> CreateCourseAsync(CreateCourseDto createCourseDto)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<string>(HttpStatusCode.BadRequest, "CenterId дар токен ёфт нашуд");

            string imagePath = string.Empty;
            if (createCourseDto.ImageFile != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(
                    createCourseDto.ImageFile, 
                    uploadPath,
                    "courses",
                    "course");

                if (imageResult.StatusCode != (int)HttpStatusCode.OK)
                    return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);

                imagePath = imageResult.Data;
            }

            var course = new Course
            {
                CourseName = createCourseDto.CourseName,
                Description = createCourseDto.Description,
                DurationInMonth = createCourseDto.DurationInMonth,
                Price = createCourseDto.Price,
                Status = createCourseDto.Status,
                CenterId = centerId.Value,
                ImagePath = imagePath,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await context.Courses.AddAsync(course);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.Created, "Курс бо муваффақият сохта шуд")
                : new Response<string>(HttpStatusCode.BadRequest, "Хатогӣ ҳангоми сохтани курс");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми сохтани курс: {ex.Message}");
        }
    }

    public async Task<Response<string>> UpdateCourseAsync(UpdateCourseDto updateCourseDto)
    {
        try
        {
            var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == updateCourseDto.Id && !c.IsDeleted);
            if (course == null)
                return new Response<string>(HttpStatusCode.NotFound, "Курс ёфт нашуд");

            if (course.CenterId != updateCourseDto.CenterId)
            {
                var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == updateCourseDto.CenterId && !c.IsDeleted);
                if (center == null)
                    return new Response<string>(HttpStatusCode.BadRequest, "Маркази таълимӣ ёфт нашуд");
            }

            if (updateCourseDto.ImageFile != null)
            {
                if (!string.IsNullOrEmpty(course.ImagePath))
                {
                    FileDeleteHelper.DeleteFile(course.ImagePath, uploadPath);
                }

                var imageResult = await FileUploadHelper.UploadFileAsync(
                    updateCourseDto.ImageFile, 
                    uploadPath,
                    "courses",
                    "course",
                    true,
                    course.ImagePath);

                if (imageResult.StatusCode != (int)HttpStatusCode.OK)
                    return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);

                course.ImagePath = imageResult.Data;
            }

            course.CourseName = updateCourseDto.CourseName;
            course.Description = updateCourseDto.Description;
            course.DurationInMonth = updateCourseDto.DurationInMonth;
            course.Price = updateCourseDto.Price;
            course.Status = updateCourseDto.Status;
            course.CenterId = updateCourseDto.CenterId;
            course.UpdatedAt = DateTimeOffset.UtcNow;

            context.Courses.Update(course);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Курс бо муваффақият навсозӣ шуд")
                : new Response<string>(HttpStatusCode.BadRequest, "Хатогӣ ҳангоми навсозии курс");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми навсозии курс: {ex.Message}");
        }
    }

    public async Task<Response<string>> DeleteCourseAsync(int id)
    {
        try
        {
            var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (course == null)
                return new Response<string>(HttpStatusCode.NotFound, "Курс ёфт нашуд");

            course.IsDeleted = true;
            course.UpdatedAt = DateTimeOffset.UtcNow;

            if (!string.IsNullOrEmpty(course.ImagePath))
            {
                FileDeleteHelper.DeleteFile(course.ImagePath, uploadPath);
            }

            context.Courses.Update(course);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Курс бо муваффақият нест карда шуд")
                : new Response<string>(HttpStatusCode.BadRequest, "Хатогӣ ҳангоми несткунии курс");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми несткунии курс: {ex.Message}");
        }
    }

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
                : new Response<List<GetCourseDto>>(HttpStatusCode.NotFound, "Курсҳо ёфт нашуданд");
        }
        catch (Exception ex)
        {
            return new Response<List<GetCourseDto>>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми гирифтани курсҳо: {ex.Message}");
        }
    }

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
                return new Response<GetCourseDto>(HttpStatusCode.NotFound, "Курс ёфт нашуд");

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
            return new Response<GetCourseDto>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми гирифтани курс: {ex.Message}");
        }
    }

    public async Task<PaginationResponse<List<GetCourseDto>>> GetCoursesPagination(CourseFilter filter)
    {
        try
        {
            var coursesQuery = context.Courses
                .Include(c => c.Center)
                .Where(c => !c.IsDeleted)
                .AsQueryable();
            coursesQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
                coursesQuery, httpContextAccessor, c => c.CenterId);

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
            return new PaginationResponse<List<GetCourseDto>>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми гирифтани курсҳо: {ex.Message}");
        }
    }

    public async Task<Response<List<GetCourseDto>>> GetCoursesByMentorAsync(int mentorId)
    {
        try
        {
            var mentor = await context.Mentors
                .Include(m => m.Groups)
                .ThenInclude(g => g.Course)
                .ThenInclude(c => c.Center)
                .FirstOrDefaultAsync(m => m.Id == mentorId && !m.IsDeleted);

            if (mentor == null)
                return new Response<List<GetCourseDto>>(HttpStatusCode.NotFound, "Устод ёфт нашуд");

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
                : new Response<List<GetCourseDto>>(HttpStatusCode.NotFound, "Барои ин устод курсҳо ёфт нашуданд");
        }
        catch (Exception ex)
        {
            return new Response<List<GetCourseDto>>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми гирифтани курсҳои устод: {ex.Message}");
        }
    }

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
            return new Response<GetCourseGroupsDto>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми гирифтани гурӯҳҳои курс: {ex.Message}");
        }
    }
}