using System.Net;
using Domain.DTOs.Center;
using Domain.DTOs.Classroom;
using Domain.DTOs.Course;
using Domain.DTOs.Group;
using Domain.Entities;
using Domain.Enums;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class GroupService(DataContext context, string uploadPath, IHttpContextAccessor httpContextAccessor)
    : IGroupService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<Response<string>> CreateGroupAsync(CreateGroupDto request)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            if (centerId == null)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "CenterId дар токен ёфт нашуд"
                };
            }

            // Check if mentor exists and belongs to the same center
            var mentorExists = await context.Mentors.AnyAsync(m => m.Id == request.MentorId && 
                                                                   m.CenterId == centerId && 
                                                                   !m.IsDeleted);
            if (!mentorExists)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Омӯзгор ёфт нашуд ё ба ин маркази таълимӣ тааллуқ надорад"
                };
            }

            // Check if course exists and belongs to the same center
            var courseExists = await context.Courses.AnyAsync(c => c.Id == request.CourseId && 
                                                                   c.CenterId == centerId && 
                                                                   !c.IsDeleted);
            if (!courseExists)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Курс ёфт нашуд ё ба ин маркази таълимӣ тааллуқ надорад"
                };
            }

            if (request.ClassroomId.HasValue)
            {
                var classroomExists = await context.Classrooms
                    .AnyAsync(c => c.Id == request.ClassroomId && 
                                  c.CenterId == centerId && 
                                  c.IsActive && 
                                  !c.IsDeleted);
                if (!classroomExists)
                {
                    return new Response<string>
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Message = "Синфхона ёфт нашуд, фаъол нест ё ба ин маркази таълимӣ тааллуқ надорад"
                    };
                }
            }

            var existingGroup = await context.Groups
                .AnyAsync(g => g.Name.ToLower() == request.Name.ToLower() && !g.IsDeleted);
            if (existingGroup)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Гурӯҳ бо ҳамин ном аллакай мавҷуд аст"
                };
            }

            string? imagePath = null;
            if (request.Image != null)
            {
                var uploadResult = await FileUploadHelper.UploadFileAsync(
                    request.Image, uploadPath, "groups", "group");
                if (uploadResult.StatusCode != (int)HttpStatusCode.OK)
                {
                    return new Response<string>
                    {
                        StatusCode = uploadResult.StatusCode,
                        Message = uploadResult.Message
                    };
                }
                imagePath = uploadResult.Data;
            }
            // Calculate DurationMonth and LessonInWeek automatically
            var startDate = request.StartDate?.ToUniversalTime() ?? DateTimeOffset.UtcNow.AddDays(1);
            var endDate = request.EndDate?.ToUniversalTime() ?? DateTimeOffset.UtcNow.AddMonths(3);
            
            var calculatedDurationMonth = (int)Math.Ceiling((endDate - startDate).TotalDays / 30.0);
            var calculatedLessonInWeek = request.ParsedLessonDays?.Count ?? 0;
            var totalWeeks = (int)Math.Ceiling((endDate - startDate).TotalDays / 7.0);

            var group = new Group
            {
                Name = request.Name,
                Description = request.Description,
                CourseId = request.CourseId,
                DurationMonth = calculatedDurationMonth,
                LessonInWeek = calculatedLessonInWeek,
                HasWeeklyExam = request.HasWeeklyExam,
                TotalWeeks = totalWeeks,
                MentorId = request.MentorId,
                ClassroomId = request.ClassroomId,
                PhotoPath = imagePath,
                Status = ActiveStatus.Inactive, 
                StartDate = startDate, 
                EndDate = endDate,
                // Automatic Lesson Scheduling
                LessonDays = request.LessonDays,
                LessonStartTime = request.LessonStartTime,
                LessonEndTime = request.LessonEndTime,
                AutoGenerateLessons = request.AutoGenerateLessons,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            context.Groups.Add(group);
            await context.SaveChangesAsync();
            if (request.AutoGenerateLessons && 
                request.ParsedLessonDays != null && request.ParsedLessonDays.Count > 0 &&
                request.LessonStartTime.HasValue && request.LessonEndTime.HasValue &&
                request.StartDate.HasValue && request.EndDate.HasValue)
            {
                var lessonGenerationResult = await LessonSchedulingHelper.GenerateSchedulesAndLessonsAsync(
                    context,
                    group,
                    request.StartDate.Value,
                    request.EndDate.Value,
                    request.ParsedLessonDays,
                    request.LessonStartTime.Value,
                    request.LessonEndTime.Value);

                if (lessonGenerationResult.StatusCode != 200)
                {
                    return new Response<string>
                    {
                        StatusCode = (int)HttpStatusCode.Created,
                        Data = "Гурӯҳ сохта шуд, аммо дарсҳо автоматӣ эҷод нашуданд",
                        Message = $"Гурӯҳ сохта шуд, аммо дарсҳо автоматӣ эҷод нашуданд: {lessonGenerationResult.Message}"
                    };
                }

                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.Created,
                    Data = $"Гурӯҳ ва {lessonGenerationResult.Data?.Count ?? 0} дарс бо муваффақият сохта шуданд",
                    Message = "Гурӯҳ ва дарсҳо бо муваффақият сохта шуданд"
                };
            }

            return new Response<string>
            {
                StatusCode = (int)HttpStatusCode.Created,
                Data = "Гурӯҳ бо муваффақият сохта шуд",
                Message = "Гурӯҳ бо муваффақият сохта шуд"
            };
        }
        catch (Exception ex)
        {
            return new Response<string>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми сохтани гурӯҳ: {ex.Message}"
            };
        }
    }

    public async Task<Response<string>> UpdateGroupAsync(int id, UpdateGroupDto request)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            if (centerId == null)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "CenterId дар токен ёфт нашуд"
                };
            }

            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

            if (group == null)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Гурӯҳ ёфт нашуд"
                };
            }

            // Check if mentor exists and belongs to the same center
            var mentorExists = await context.Mentors.AnyAsync(m => m.Id == request.MentorId && 
                                                                   m.CenterId == centerId && 
                                                                   !m.IsDeleted);
            if (!mentorExists)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Омӯзгор ёфт нашуд ё ба ин маркази таълимӣ тааллуқ надорад"
                };
            }

            // Check if course exists and belongs to the same center
            var courseExists = await context.Courses.AnyAsync(c => c.Id == request.CourseId && 
                                                                   c.CenterId == centerId && 
                                                                   !c.IsDeleted);
            if (!courseExists)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Курс ёфт нашуд ё ба ин маркази таълимӣ тааллуқ надорад"
                };
            }

            if (request.ClassroomId.HasValue)
            {
                var classroomExists = await context.Classrooms
                    .AnyAsync(c => c.Id == request.ClassroomId && 
                                  c.CenterId == centerId && 
                                  c.IsActive && 
                                  !c.IsDeleted);
                if (!classroomExists)
                {
                    return new Response<string>
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Message = "Синфхона ёфт нашуд, фаъол нест ё ба ин маркази таълимӣ тааллуқ надорад"
                    };
                }
            }

            // Check for duplicate name (excluding current group)
            var existingGroup = await context.Groups
                .AnyAsync(g => g.Name.ToLower() == request.Name.ToLower() && 
                              g.Id != id && !g.IsDeleted);
            if (existingGroup)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Гурӯҳ бо ҳамин ном аллакай мавҷуд аст"
                };
            }

            group.Name = request.Name;
            group.Description = request.Description;
            group.CourseId = request.CourseId;
            group.MentorId = request.MentorId;
            group.ClassroomId = request.ClassroomId;
            group.HasWeeklyExam = request.HasWeeklyExam;
            group.StartDate = request.StartDate.Value;
            group.EndDate = request.EndDate.Value;
            group.LessonDays = request.LessonDays;
            group.LessonStartTime = request.LessonStartTime;
            group.LessonEndTime = request.LessonEndTime;
            group.AutoGenerateLessons = request.AutoGenerateLessons;
            
            if (request.DurationMonth.HasValue)
                group.DurationMonth = request.DurationMonth.Value;
                
            if (request.LessonInWeek.HasValue)
                group.LessonInWeek = request.LessonInWeek.Value;

            // Handle image update
            if (request.Image != null)
            {
      
                if (!string.IsNullOrEmpty(group.PhotoPath))
                {
                    FileDeleteHelper.DeleteFile(group.PhotoPath, uploadPath);
                }

                var imagePath = await FileUploadHelper.UploadFileAsync(
                    request.Image, 
                    uploadPath,
                    "groups",
                    "group");

                if (imagePath.StatusCode != (int)HttpStatusCode.OK)
                {
                    return new Response<string>(HttpStatusCode.OK,"Succses");
                }
                group.PhotoPath = imagePath.Data;
            }

            await context.SaveChangesAsync();

            return new Response<string>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = "Гурӯҳ бо муваффақият навсозӣ шуд",
                Message = "Гурӯҳ бо муваффақият навсозӣ шуд"
            };
        }
        catch (Exception ex)
        {
            return new Response<string>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми навсозии гурӯҳ: {ex.Message}"
            };
        }
    }

    public async Task<Response<string>> DeleteGroupAsync(int id)
    {
        try
        {
            var group = await context.Groups
                .Include(g => g.StudentGroups)
                .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

            if (group == null)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Гурӯҳ ёфт нашуд"
                };
            }

            if (group.StudentGroups.Any(sg => !sg.IsDeleted))
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Гурӯҳро нест кардан мумкин нест, зеро донишҷӯёни фаъол дорад"
                };
            }

            group.IsDeleted = true;
            group.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();

            return new Response<string>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = "Гурӯҳ бо муваффақият нест карда шуд",
                Message = "Гурӯҳ бо муваффақият нест карда шуд"
            };
        }
        catch (Exception ex)
        {
            return new Response<string>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми несткунии гурӯҳ: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetGroupDto>> GetGroupByIdAsync(int id)
    {
        try
        {
            var group = await context.Groups
                .Include(g => g.Course)
                .Include(g => g.Mentor)
                .Include(g => g.Classroom)
                .ThenInclude(c => c.Center)
                .Include(g => g.StudentGroups.Where(sg => !sg.IsDeleted))
                .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

            if (group == null)
            {
                return new Response<GetGroupDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Гурӯҳ ёфт нашуд"
                };
            }

            var groupDto = MapToGetGroupDto(group);

            return new Response<GetGroupDto>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = groupDto
            };
        }
        catch (Exception ex)
        {
            return new Response<GetGroupDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани гурӯҳ: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetGroupDto>>> GetGroups()
    {
        try
        {
            var groups = await context.Groups
                .Include(g => g.Course)
                .Include(g => g.Mentor)
                .Include(g => g.Classroom)
                .ThenInclude(c => c.Center)
                .Include(g => g.StudentGroups.Where(sg => !sg.IsDeleted))
                .Where(g => !g.IsDeleted)
                .OrderBy(g => g.Name)
                .ToListAsync();

            var groupDtos = groups.Select(MapToGetGroupDto).ToList();

            return new Response<List<GetGroupDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = groupDtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetGroupDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани гурӯҳҳо: {ex.Message}"
            };
        }
    }

    public async Task<PaginationResponse<List<GetGroupDto>>> GetGroupPaginated(GroupFilter filter)
    {
        try
        {
            var query = context.Groups
                .Include(g => g.Course)
                .Include(g => g.Mentor)
                .Include(g => g.Classroom)
                .ThenInclude(c => c.Center)
                .Include(g => g.StudentGroups.Where(sg => !sg.IsDeleted))
                .Where(g => !g.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(g => g.Name.Contains(filter.Search) ||
                                        g.Description.Contains(filter.Search));
            }

            if (filter.CourseId.HasValue)
            {
                query = query.Where(g => g.CourseId == filter.CourseId);
            }

            if (filter.MentorId.HasValue)
            {
                query = query.Where(g => g.MentorId == filter.MentorId);
            }

            if (filter.ClassroomId.HasValue)
            {
                query = query.Where(g => g.ClassroomId == filter.ClassroomId);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(g => g.Status == filter.Status);
            }

            var totalRecords = await query.CountAsync();
            var groups = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .OrderBy(g => g.Name)
                .ToListAsync();

            var groupDtos = groups.Select(MapToGetGroupDto).ToList();

            return new PaginationResponse<List<GetGroupDto>>(groupDtos, totalRecords, filter.PageNumber, filter.PageSize)
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetGroupDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани гурӯҳҳо: {ex.Message}"
            };
        }
    }
    
    private GetGroupDto MapToGetGroupDto(Group group)
    {
        return new GetGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            DurationMonth = group.DurationMonth,
            LessonInWeek = group.LessonInWeek,
            TotalWeeks = group.TotalWeeks,
            Started = group.Started,
            CurrentStudentsCount = group.StudentGroups?.Count(sg => !sg.IsDeleted) ?? 0,
            Status = group.Status,
            StartDate = group.StartDate,
            EndDate = group.EndDate,
            Mentor = group.Mentor != null ? new Domain.DTOs.Student.GetSimpleDto
            {
                Id = group.Mentor.Id,
                FullName = group.Mentor.FullName
            } : null,
            DayOfWeek = 0, 
            Course = new GetSimpleCourseDto()
            {
                Id = group.Course.Id,
                CourseName = group.Course.CourseName,
            },
            ImagePath = group.PhotoPath,
            CurrentWeek = group.CurrentWeek,
            ClassroomId = group.ClassroomId,
            Classroom = group.Classroom != null ? new GetClassroomDto
            {
                Id = group.Classroom.Id,
                Name = group.Classroom.Name,
                Description = group.Classroom.Description,
                Capacity = group.Classroom.Capacity,
                IsActive = group.Classroom.IsActive,
                Center = group.Classroom.Center != null ? new GetCenterSimpleDto
                {
                    Id = group.Classroom.Center.Id,
                    Name = group.Classroom.Center.Name
                } : null,
                CreatedAt = group.Classroom.CreatedAt,
                UpdatedAt = group.Classroom.UpdatedAt
            } : null,
            LessonDays = !string.IsNullOrEmpty(group.LessonDays) 
                ? group.LessonDays 
                : null,
            LessonStartTime = group.LessonStartTime,
            LessonEndTime = group.LessonEndTime,
            AutoGenerateLessons = group.AutoGenerateLessons,
            
        };
    }
} 