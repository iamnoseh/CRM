using System.Net;
using Domain.DTOs.Classroom;
using Domain.DTOs.Group;
using Domain.DTOs.Lesson;
using Domain.DTOs.Schedule;
using Domain.Entities;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class LessonService(DataContext context) : ILessonService
{
    public async Task<Response<GetLessonDto>> CreateLessonAsync(CreateLessonDto createDto)
    {
        try
        {
            var groupExists = await context.Groups.AnyAsync(g => g.Id == createDto.GroupId && !g.IsDeleted);
            if (!groupExists)
            {
                return new Response<GetLessonDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Гурӯҳ ёфт нашуд"
                };
            }

            if (createDto.ClassroomId.HasValue)
            {
                var classroomExists = await context.Classrooms
                    .AnyAsync(c => c.Id == createDto.ClassroomId && c.IsActive && !c.IsDeleted);
                if (!classroomExists)
                {
                    return new Response<GetLessonDto>
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Message = "Синфхона ёфт нашуд ё фаъол нест"
                    };
                }

                // Check if classroom is available
                var canSchedule = await CanScheduleLessonAsync(createDto.ClassroomId.Value, createDto.StartTime, createDto.EndTime);
                if (canSchedule.StatusCode != 200 || !canSchedule.Data)
                {
                    return new Response<GetLessonDto>
                    {
                        StatusCode = (int)HttpStatusCode.Conflict,
                        Message = "Синфхона дар ин вақт ишғол аст"
                    };
                }
            }

            // Check if schedule exists (if provided)
            if (createDto.ScheduleId.HasValue)
            {
                var scheduleExists = await context.Schedules
                    .AnyAsync(s => s.Id == createDto.ScheduleId && !s.IsDeleted);
                if (!scheduleExists)
                {
                    return new Response<GetLessonDto>
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Message = "Ҷадвали дарс ёфт нашуд"
                    };
                }
            }

            var lesson = new Lesson
            {
                GroupId = createDto.GroupId,
                StartTime = createDto.StartTime,
                EndTime = createDto.EndTime,
                ClassroomId = createDto.ClassroomId,
                ScheduleId = createDto.ScheduleId,
                WeekIndex = createDto.WeekIndex,
                DayOfWeekIndex = createDto.DayOfWeekIndex,
                DayIndex = createDto.DayIndex,
                Notes = createDto.Notes,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            context.Lessons.Add(lesson);
            await context.SaveChangesAsync();

            return await GetLessonByIdAsync(lesson.Id);
        }
        catch (Exception ex)
        {
            return new Response<GetLessonDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми сохтани дарс: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetLessonDto>> GetLessonByIdAsync(int id)
    {
        try
        {
            var lesson = await context.Lessons
                .Include(l => l.Group)
                .Include(l => l.Classroom)
                .ThenInclude(c => c.Center)
                .Include(l => l.Schedule)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            if (lesson == null)
            {
                return new Response<GetLessonDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Дарс ёфт нашуд"
                };
            }

            var lessonDto = MapToGetLessonDto(lesson);

            return new Response<GetLessonDto>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = lessonDto
            };
        }
        catch (Exception ex)
        {
            return new Response<GetLessonDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани дарс: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetLessonDto>>> GetLessonsByGroupAsync(int groupId)
    {
        try
        {
            var lessons = await context.Lessons
                .Include(l => l.Group)
                .Include(l => l.Classroom)
                .ThenInclude(c => c.Center)
                .Include(l => l.Schedule)
                .Where(l => l.GroupId == groupId && !l.IsDeleted)
                .OrderBy(l => l.StartTime)
                .ToListAsync();

            var lessonDtos = lessons.Select(MapToGetLessonDto).ToList();

            return new Response<List<GetLessonDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = lessonDtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetLessonDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани дарсҳои гурӯҳ: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetLessonDto>>> GetLessonsByClassroomAsync(int classroomId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        try
        {
            startDate ??= DateOnly.FromDateTime(DateTime.UtcNow.Date);
            endDate ??= startDate.Value.AddDays(6);

            var startDateTime = startDate.Value.ToDateTime(TimeOnly.MinValue);
            var endDateTime = endDate.Value.ToDateTime(TimeOnly.MaxValue);

            var lessons = await context.Lessons
                .Include(l => l.Group)
                .Include(l => l.Classroom)
                .ThenInclude(c => c.Center)
                .Include(l => l.Schedule)
                .Where(l => l.ClassroomId == classroomId &&
                           !l.IsDeleted &&
                           l.StartTime >= startDateTime &&
                           l.StartTime <= endDateTime)
                .OrderBy(l => l.StartTime)
                .ToListAsync();

            var lessonDtos = lessons.Select(MapToGetLessonDto).ToList();

            return new Response<List<GetLessonDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = lessonDtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetLessonDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани дарсҳои синфхона: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetLessonDto>>> GetLessonsByScheduleAsync(int scheduleId)
    {
        try
        {
            var lessons = await context.Lessons
                .Include(l => l.Group)
                .Include(l => l.Classroom)
                .ThenInclude(c => c.Center)
                .Include(l => l.Schedule)
                .Where(l => l.ScheduleId == scheduleId && !l.IsDeleted)
                .OrderBy(l => l.StartTime)
                .ToListAsync();

            var lessonDtos = lessons.Select(MapToGetLessonDto).ToList();

            return new Response<List<GetLessonDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = lessonDtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetLessonDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани дарсҳои ҷадвал: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetLessonDto>> UpdateLessonAsync(UpdateLessonDto updateDto)
    {
        try
        {
            var lesson = await context.Lessons
                .FirstOrDefaultAsync(l => l.Id == updateDto.Id && !l.IsDeleted);

            if (lesson == null)
            {
                return new Response<GetLessonDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Дарс ёфт нашуд"
                };
            }

            // Check classroom availability if changed
            if (updateDto.ClassroomId.HasValue && updateDto.ClassroomId != lesson.ClassroomId)
            {
                var classroomExists = await context.Classrooms
                    .AnyAsync(c => c.Id == updateDto.ClassroomId && c.IsActive && !c.IsDeleted);
                if (!classroomExists)
                {
                    return new Response<GetLessonDto>
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Message = "Синфхона ёфт нашуд ё фаъол нест"
                    };
                }

                // Check if the new time slot conflicts with existing lessons
                var hasConflict = await context.Lessons
                    .AnyAsync(l => l.ClassroomId == updateDto.ClassroomId &&
                                  l.Id != updateDto.Id &&
                                  !l.IsDeleted &&
                                  l.StartTime < updateDto.EndTime &&
                                  l.EndTime > updateDto.StartTime);

                if (hasConflict)
                {
                    return new Response<GetLessonDto>
                    {
                        StatusCode = (int)HttpStatusCode.Conflict,
                        Message = "Синфхона дар ин вақт ишғол аст"
                    };
                }
            }

            lesson.StartTime = updateDto.StartTime;
            lesson.EndTime = updateDto.EndTime;
            lesson.ClassroomId = updateDto.ClassroomId;
            lesson.ScheduleId = updateDto.ScheduleId;
            lesson.WeekIndex = updateDto.WeekIndex;
            lesson.DayOfWeekIndex = updateDto.DayOfWeekIndex;
            lesson.DayIndex = updateDto.DayIndex;
            lesson.Notes = updateDto.Notes;
            lesson.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();

            return await GetLessonByIdAsync(lesson.Id);
        }
        catch (Exception ex)
        {
            return new Response<GetLessonDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми навсозии дарс: {ex.Message}"
            };
        }
    }

    public async Task<Response<bool>> DeleteLessonAsync(int id)
    {
        try
        {
            var lesson = await context.Lessons
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            if (lesson == null)
            {
                return new Response<bool>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Дарс ёфт нашуд"
                };
            }

            lesson.IsDeleted = true;
            lesson.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();

            return new Response<bool>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = true,
                Message = "Дарс бо муваффақият нест карда шуд"
            };
        }
        catch (Exception ex)
        {
            return new Response<bool>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми несткунии дарс: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetLessonDto>>> GetWeeklyLessonsAsync(int groupId, DateOnly weekStart)
    {
        try
        {
            var weekEnd = weekStart.AddDays(6);
            var startDateTime = weekStart.ToDateTime(TimeOnly.MinValue);
            var endDateTime = weekEnd.ToDateTime(TimeOnly.MaxValue);

            var lessons = await context.Lessons
                .Include(l => l.Group)
                .Include(l => l.Classroom)
                .ThenInclude(c => c.Center)
                .Include(l => l.Schedule)
                .Where(l => l.GroupId == groupId &&
                           !l.IsDeleted &&
                           l.StartTime >= startDateTime &&
                           l.StartTime <= endDateTime)
                .OrderBy(l => l.StartTime)
                .ToListAsync();

            var lessonDtos = lessons.Select(MapToGetLessonDto).ToList();

            return new Response<List<GetLessonDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = lessonDtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetLessonDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани дарсҳои ҳафтавӣ: {ex.Message}"
            };
        }
    }

    public async Task<Response<bool>> CanScheduleLessonAsync(int classroomId, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        try
        {
            var hasConflict = await context.Lessons
                .AnyAsync(l => l.ClassroomId == classroomId &&
                              !l.IsDeleted &&
                              l.StartTime < endTime &&
                              l.EndTime > startTime);

            return new Response<bool>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = !hasConflict
            };
        }
        catch (Exception ex)
        {
            return new Response<bool>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми санҷиши дастрасии синфхона: {ex.Message}"
            };
        }
    }

    private GetLessonDto MapToGetLessonDto(Lesson lesson)
    {
        return new GetLessonDto
        {
            Id = lesson.Id,
            StartTime = lesson.StartTime,
            EndTime = lesson.EndTime,
            GroupId = lesson.GroupId,
            Group = new GetGroupDto
            {
                Id = lesson.Group.Id,
                Name = lesson.Group.Name,
                Description = lesson.Group.Description
            },
            ClassroomId = lesson.ClassroomId,
            Classroom = lesson.Classroom != null ? new GetClassroomDto
            {
                Id = lesson.Classroom.Id,
                Name = lesson.Classroom.Name,
                Description = lesson.Classroom.Description,
                Capacity = lesson.Classroom.Capacity,
                IsActive = lesson.Classroom.IsActive,
                CenterId = lesson.Classroom.CenterId,
                Center = lesson.Classroom.Center != null ? new Domain.DTOs.Center.GetCenterSimpleDto
                {
                    Id = lesson.Classroom.Center.Id,
                    Name = lesson.Classroom.Center.Name
                } : null,
                CreatedAt = lesson.Classroom.CreatedAt,
                UpdatedAt = lesson.Classroom.UpdatedAt
            } : null,
            ScheduleId = lesson.ScheduleId,
            Schedule = lesson.Schedule != null ? new GetScheduleDto
            {
                Id = lesson.Schedule.Id,
                StartTime = lesson.Schedule.StartTime,
                EndTime = lesson.Schedule.EndTime,
                DayOfWeek = lesson.Schedule.DayOfWeek,
                StartDate = lesson.Schedule.StartDate,
                EndDate = lesson.Schedule.EndDate
            } : null,
            WeekIndex = lesson.WeekIndex,
            DayOfWeekIndex = lesson.DayOfWeekIndex,
            DayIndex = lesson.DayIndex,
            Notes = lesson.Notes,
            CreatedAt = lesson.CreatedAt,
            UpdatedAt = lesson.UpdatedAt
        };
    }
} 