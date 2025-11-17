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
using System.Security.Claims;

namespace Infrastructure.Services;

public class GroupService(DataContext context, string uploadPath, IHttpContextAccessor httpContextAccessor)
    : IGroupService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private async Task<int?> GetEffectiveCenterIdAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var roles = user?.Claims
            .Where(c => c.Type == ClaimTypes.Role || string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .ToList();
        if (roles != null && roles.Any(r => string.Equals(r, "SuperAdmin", StringComparison.OrdinalIgnoreCase)))
            return null;

        var centerIdClaim = user?.Claims.FirstOrDefault(c => c.Type == "CenterId")?.Value;
        if (int.TryParse(centerIdClaim, out var centerIdFromClaim))
            return centerIdFromClaim;

        var userIdRaw = user?.FindFirst("UserId")?.Value
                        ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdRaw, out var userId) && userId > 0)
        {
            var dbCenterId = await context.Users
                .Where(u => u.Id == userId && !u.IsDeleted)
                .Select(u => u.CenterId)
                .FirstOrDefaultAsync();
            return dbCenterId;
        }
        return null;
    }

    public async Task<Response<string>> CreateGroupAsync(CreateGroupDto request)
    {
        try
        {
            var centerId = await GetEffectiveCenterIdAsync();
            if (centerId == null)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "CenterId дар токен ёфт нашуд"
                };
            }

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

            var duplicateQuery = context.Groups
                .Where(g => g.Name.ToLower() == request.Name.ToLower() && !g.IsDeleted);
            if (centerId != null)
            {
                duplicateQuery = duplicateQuery.Where(g => g.Course!.CenterId == centerId);
            }
            var existingGroup = await duplicateQuery.AnyAsync();
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
                HasWeeklyExam = request.HasWeeklyExam ?? false,
                TotalWeeks = totalWeeks,
                MentorId = request.MentorId,
                ClassroomId = request.ClassroomId,
                PhotoPath = imagePath,
                Status = ActiveStatus.Inactive, 
                StartDate = startDate, 
                EndDate = endDate,
                LessonDays = request.LessonDays,
                LessonStartTime = request.LessonStartTime,
                LessonEndTime = request.LessonEndTime,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            context.Groups.Add(group);
            await context.SaveChangesAsync();

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
            var centerId = await GetEffectiveCenterIdAsync();
            if (centerId == null)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "CenterId дар токен ёфт нашуд"
                };
            }

            var group = await context.Groups
                .Include(g => g.Course)
                .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted && g.Course!.CenterId == centerId);

            if (group == null)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Гурӯҳ ёфт нашуд"
                };
            }

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
                .Include(g => g.Course)
                .AnyAsync(g => g.Name.ToLower() == request.Name.ToLower() && 
                              g.Id != id && !g.IsDeleted && g.Course!.CenterId == centerId);
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
            group.HasWeeklyExam = request.HasWeeklyExam ?? false;
            group.StartDate = request.StartDate.Value;
            group.EndDate = request.EndDate.Value;
            group.LessonDays = request.LessonDays;
            group.LessonStartTime = request.LessonStartTime;
            group.LessonEndTime = request.LessonEndTime;
            group.Status = request.Status;
            
            var startDate = request.StartDate.Value;
            var endDate = request.EndDate.Value;

            group.DurationMonth = (int)Math.Ceiling((endDate - startDate).TotalDays / 30.0);
            group.LessonInWeek = request.ParsedLessonDays?.Count ?? 0;
            group.TotalWeeks = (int)Math.Ceiling((endDate - startDate).TotalDays / 7.0);

            if (request.DurationMonth.HasValue)
                group.DurationMonth = request.DurationMonth.Value;
                
            if (request.LessonInWeek.HasValue)
                group.LessonInWeek = request.LessonInWeek.Value;

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
            var delQuery = context.Groups
                .Include(g => g.StudentGroups)
                .Include(g => g.Course)
                .Where(g => !g.IsDeleted)
                .AsQueryable();
            delQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(delQuery, _httpContextAccessor, g => (int?)g.Course!.CenterId);

            var group = await delQuery.FirstOrDefaultAsync(g => g.Id == id);

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
            var queryById = context.Groups
                .Include(g => g.Course)
                .Include(g => g.Mentor)
                .Include(g => g.Classroom)
                .ThenInclude(c => c.Center)
                .Include(g => g.StudentGroups.Where(sg => !sg.IsDeleted))
                .Where(g => !g.IsDeleted)
                .AsQueryable();
            queryById = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(queryById, _httpContextAccessor, g => (int?)g.Course!.CenterId);

            var group = await queryById.FirstOrDefaultAsync(g => g.Id == id);

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
            var queryAll = context.Groups
                .Include(g => g.Course)
                .Include(g => g.Mentor)
                .Include(g => g.Classroom)
                .ThenInclude(c => c.Center)
                .Include(g => g.StudentGroups.Where(sg => !sg.IsDeleted))
                .Where(g => !g.IsDeleted)
                .AsQueryable();
            queryAll = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(queryAll, _httpContextAccessor, g => (int?)g.Course!.CenterId);

            var groups = await queryAll
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
            query = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(query, _httpContextAccessor, g => (int?)g.Course!.CenterId);

            var user = _httpContextAccessor.HttpContext?.User;
            var principalType = user?.FindFirst("PrincipalType")?.Value;
            var nameIdStr = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(nameIdStr, out var principalId);

            var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool isAdminLike = roles.Contains("Admin") || roles.Contains("SuperAdmin") || roles.Contains("Manager");
            bool isTeacherLike = roles.Contains("Mentor");

            if (!isAdminLike && principalId > 0)
            {
                if (string.Equals(principalType, "Student", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(g => g.StudentGroups.Any(sg => sg.StudentId == principalId && !sg.IsDeleted));
                }
                else if (string.Equals(principalType, "Mentor", StringComparison.OrdinalIgnoreCase) || isTeacherLike)
                {
                    query = query.Where(g => g.MentorId == principalId);
                }
            }
            
            if (!string.IsNullOrEmpty(filter.Name))
            {
                query = query.Where(g => g.Name.Contains(filter.Name));
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

            if (filter.Started.HasValue)
            {
                query = query.Where(g => g.Started == filter.Started);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(g => g.Status == filter.Status);
            }

            if (filter.StartDateFrom.HasValue)
            {
                query = query.Where(g => g.StartDate >= filter.StartDateFrom);
            }

            if (filter.StartDateTo.HasValue)
            {
                query = query.Where(g => g.StartDate <= filter.StartDateTo);
            }

            if (filter.EndDateFrom.HasValue)
            {
                query = query.Where(g => g.EndDate >= filter.EndDateFrom);
            }

            if (filter.EndDateTo.HasValue)
            {
                query = query.Where(g => g.EndDate <= filter.EndDateTo);
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
    
    public async Task<Response<List<GetGroupDto>>> GetGroupsByStudentIdAsync(int studentId)
    {
        try
        {
            var byStudentQuery = context.Groups
                .Include(g => g.Course)
                .Include(g => g.Mentor)
                .Include(g => g.Classroom)
                .ThenInclude(c => c.Center)
                .Include(g => g.StudentGroups.Where(sg => !sg.IsDeleted && sg.StudentId == studentId))
                .Where(g => !g.IsDeleted && g.StudentGroups.Any(sg => sg.StudentId == studentId && !sg.IsDeleted))
                .AsQueryable();
            byStudentQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(byStudentQuery, _httpContextAccessor, g => (int?)g.Course!.CenterId);

            var groups = await byStudentQuery
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
                Message = $"Хатогӣ ҳангоми гирифтани гурӯҳҳо бо studentId: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetGroupDto>>> GetGroupsByMentorIdAsync(int mentorId)
    {
        try
        {
            var byMentorQuery = context.Groups
                .Include(g => g.Course)
                .Include(g => g.Mentor)
                .Include(g => g.Classroom)
                .ThenInclude(c => c.Center)
                .Include(g => g.StudentGroups.Where(sg => !sg.IsDeleted))
                .Where(g => !g.IsDeleted && g.MentorId == mentorId)
                .AsQueryable();
            byMentorQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(byMentorQuery, _httpContextAccessor, g => (int?)g.Course!.CenterId);

            var groups = await byMentorQuery
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
                Message = $"Хатогӣ ҳангоми гирифтани гурӯҳҳо бо mentorId: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetSimpleGroupInfoDto>>> GetGroupsSimpleAsync(string? search)
    {
        try
        {
            var query = context.Groups
                .Where(g => !g.IsDeleted)
                .AsQueryable();
            query = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(query, _httpContextAccessor, g => (int?)g.Course!.CenterId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                var sLower = s.ToLower();
                if (int.TryParse(s, out var idNumeric) && idNumeric > 0)
                {
                    query = query.Where(g => g.Id == idNumeric || g.Name.ToLower().Contains(sLower));
                }
                else
                {
                    query = query.Where(g => g.Name.ToLower().Contains(sLower));
                }
            }

            var simple = await query
                .OrderBy(g => g.Name)
                .Select(g => new GetSimpleGroupInfoDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    ImagePath = g.PhotoPath
                })
                .ToListAsync();

            return new Response<List<GetSimpleGroupInfoDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = simple
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetSimpleGroupInfoDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани рӯйхати соддаи гурӯҳҳо: {ex.Message}"
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
            HasWeeklyExam = group.HasWeeklyExam,
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
            
        };
    }
} 
