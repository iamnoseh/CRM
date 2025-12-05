using System.Net;
using Domain.DTOs.Course;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Helpers;
using Infrastructure.Constants;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CourseService(DataContext context, string uploadPath,
    IHttpContextAccessor httpContextAccessor) : ICourseService
{
    #region CreateCourseAsync

    public async Task<Response<string>> CreateCourseAsync(CreateCourseDto createCourseDto)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Group.CenterIdNotFound);

            string imagePath = string.Empty;
            if (createCourseDto.ImageFile != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(
                    createCourseDto.ImageFile,
                    uploadPath,
                    "courses",
                    "course");

                if (imageResult.StatusCode != (int)HttpStatusCode.OK)
                    if (imageResult.Message != null)
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
                ? new Response<string>(HttpStatusCode.Created, Messages.Course.Created)
                : new Response<string>(HttpStatusCode.BadRequest, Messages.Course.CreationError);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Course.CreationError, ex.Message));
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
                return new Response<string>(HttpStatusCode.NotFound, Messages.Course.NotFound);

            if (course.CenterId != updateCourseDto.CenterId)
            {
                var center = await context.Centers.FirstOrDefaultAsync(c => c.Id == updateCourseDto.CenterId && !c.IsDeleted);
                if (center == null)
                    return new Response<string>(HttpStatusCode.BadRequest, Messages.Center.NotFound);
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
                    if (imageResult.Message != null)
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
                ? new Response<string>(HttpStatusCode.OK, Messages.Course.Updated)
                : new Response<string>(HttpStatusCode.BadRequest, Messages.Course.UpdateError);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Course.UpdateError, ex.Message));
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
                return new Response<string>(HttpStatusCode.NotFound, Messages.Course.NotFound);

            course.IsDeleted = true;
            course.UpdatedAt = DateTimeOffset.UtcNow;

            if (!string.IsNullOrEmpty(course.ImagePath))
            {
                FileDeleteHelper.DeleteFile(course.ImagePath, uploadPath);
            }

            context.Courses.Update(course);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, Messages.Course.Deleted)
                : new Response<string>(HttpStatusCode.BadRequest, Messages.Course.DeleteError);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Course.DeleteError, ex.Message));
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
                    CenterName = c.Center!.Name
                })
                .ToListAsync();

            return courses.Any()
                ? new Response<List<GetCourseDto>>(courses)
                : new Response<List<GetCourseDto>>(HttpStatusCode.NotFound, Messages.Course.NotFound);
        }
        catch (Exception ex)
        {
            return new Response<List<GetCourseDto>>(HttpStatusCode.InternalServerError, ex.Message);
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
                return new Response<GetCourseDto>(HttpStatusCode.NotFound, Messages.Course.NotFound);

            var courseDto = DtoMappingHelper.MapToGetCourseDto(course);

            return new Response<GetCourseDto>(courseDto);
        }
        catch (Exception ex)
        {
            return new Response<GetCourseDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetCoursesPagination

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
                .Select(c => DtoMappingHelper.MapToGetCourseDto(c))
                .ToListAsync();

            return new PaginationResponse<List<GetCourseDto>>(
                courses,
                totalRecords,
                filter.PageNumber,
                filter.PageSize);
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetCourseDto>>(HttpStatusCode.InternalServerError, ex.Message);
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
                .ThenInclude(c => c!.Center)
                .FirstOrDefaultAsync(m => m.Id == mentorId && !m.IsDeleted);

            if (mentor == null)
                return new Response<List<GetCourseDto>>(HttpStatusCode.NotFound, Messages.Mentor.NotFound);

            var courses = mentor.Groups
                .Where(g => g is { IsDeleted: false, Course.IsDeleted: false })
                .Select(g => g.Course!)
                .Distinct()
                .AsQueryable();
            courses = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
                courses, httpContextAccessor, c => c.CenterId);
            var courseDtos = courses.Select(c => DtoMappingHelper.MapToGetCourseDto(c)).ToList();

            return courseDtos.Any()
                ? new Response<List<GetCourseDto>>(courseDtos)
                : new Response<List<GetCourseDto>>(HttpStatusCode.NotFound, Messages.Course.NotFound);
        }
        catch (Exception ex)
        {
            return new Response<List<GetCourseDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetCourseGroupsAndCountAsync

    public async Task<Response<GetCourseGroupsDto>> GetCourseGroupsAndCountAsync(int courseId)
    {
        try
        {
            var groupsQuery = context.Groups
                .Include(g => g.Course)
                .Where(g => g.CourseId == courseId && !g.IsDeleted);
            groupsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
                groupsQuery, httpContextAccessor, g => g.Course!.CenterId);
            var groups = await groupsQuery.ToListAsync();
            var groupDtos = groups.Select(g => DtoMappingHelper.MapToGetGroupDto(g)).ToList();
            var dto = new GetCourseGroupsDto
            {
                Groups = groupDtos,
                Count = groupDtos.Count
            };
            return new Response<GetCourseGroupsDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetCourseGroupsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetSimpleCourses

    public async Task<PaginationResponse<List<GetSimpleCourseDto>>> GetSimpleCourses(BaseFilter filter)
    {
        try
        {
            var coursesQuery = context.Courses
                .Where(c => !c.IsDeleted)
                .AsQueryable();

            coursesQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
                coursesQuery, httpContextAccessor, c => c.CenterId);

            var totalRecords = await coursesQuery.CountAsync();

            var skip = (filter.PageNumber - 1) * filter.PageSize;
            var courses = await coursesQuery
                .OrderBy(c => c.CourseName)
                .Skip(skip)
                .Take(filter.PageSize)
                .Select(c => DtoMappingHelper.MapToGetSimpleCourseDto(c))
                .ToListAsync();

            return new PaginationResponse<List<GetSimpleCourseDto>>(
                courses,
                totalRecords,
                filter.PageNumber,
                filter.PageSize);
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetSimpleCourseDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion
}