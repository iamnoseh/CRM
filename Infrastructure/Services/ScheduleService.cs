using System.Net;
using Domain.DTOs.Center;
using Domain.DTOs.Classroom;
using Domain.DTOs.Group;
using Domain.DTOs.Schedule;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ScheduleService : IScheduleService
{
    private readonly DataContext _context;

    public ScheduleService(DataContext context)
    {
        _context = context;
    }

    public async Task<Response<GetScheduleDto>> CreateScheduleAsync(CreateScheduleDto createDto)
    {
        try
        {
            var conflictCheck = await CheckScheduleConflictAsync(createDto);
            if (conflictCheck.StatusCode != 200)
            {
                return new Response<GetScheduleDto>
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = conflictCheck.Message
                };
            }

            if (conflictCheck.Data?.HasConflict == true)
            {
                return new Response<GetScheduleDto>
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = "Вақти дарс бо дарсҳои дигар мутобиқат дорад"
                };
            }

            var schedule = new Schedule
            {
                ClassroomId = createDto.ClassroomId,
                GroupId = createDto.GroupId,
                StartTime = createDto.StartTime,
                EndTime = createDto.EndTime,
                DayOfWeek = createDto.DayOfWeek,
                StartDate = createDto.StartDate,
                EndDate = createDto.EndDate,
                IsRecurring = createDto.IsRecurring,
                Notes = createDto.Notes,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            return await GetScheduleByIdAsync(schedule.Id);
        }
        catch (Exception ex)
        {
            return new Response<GetScheduleDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми сохтани ҷадвали дарс: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetScheduleDto>> GetScheduleByIdAsync(int id)
    {
        try
        {
            var schedule = await _context.Schedules
                .Include(s => s.Classroom)
                .ThenInclude(c => c.Center)
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (schedule == null)
            {
                return new Response<GetScheduleDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Ҷадвали дарс ёфт нашуд"
                };
            }

            var scheduleDto = MapToGetScheduleDto(schedule);

            return new Response<GetScheduleDto>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = scheduleDto
            };
        }
        catch (Exception ex)
        {
            return new Response<GetScheduleDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани ҷадвали дарс: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetScheduleDto>>> GetSchedulesByClassroomAsync(int classroomId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        try
        {
            startDate ??= DateOnly.FromDateTime(DateTime.UtcNow.Date);
            endDate ??= startDate.Value.AddDays(6);

            var schedules = await _context.Schedules
                .Include(s => s.Classroom)
                .ThenInclude(c => c.Center)
                .Include(s => s.Group)
                .Where(s => s.ClassroomId == classroomId &&
                           s.Status == ActiveStatus.Active &&
                           !s.IsDeleted &&
                           s.StartDate <= endDate &&
                           (s.EndDate == null || s.EndDate >= startDate))
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            var scheduleDtos = schedules.Select(MapToGetScheduleDto).ToList();

            return new Response<List<GetScheduleDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = scheduleDtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetScheduleDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани ҷадвалҳои синфхона: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetScheduleDto>>> GetSchedulesByGroupAsync(int groupId)
    {
        try
        {
            var schedules = await _context.Schedules
                .Include(s => s.Classroom)
                .ThenInclude(c => c.Center)
                .Include(s => s.Group)
                .Where(s => s.GroupId == groupId &&
                           s.Status == ActiveStatus.Active &&
                           !s.IsDeleted)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            var scheduleDtos = schedules.Select(MapToGetScheduleDto).ToList();

            return new Response<List<GetScheduleDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = scheduleDtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetScheduleDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани ҷадвалҳои гурӯҳ: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetScheduleDto>> UpdateScheduleAsync(UpdateScheduleDto updateDto)
    {
        try
        {
            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.Id == updateDto.Id && !s.IsDeleted);

            if (schedule == null)
            {
                return new Response<GetScheduleDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Ҷадвали дарс ёфт нашуд"
                };
            }
            var conflictDto = new CreateScheduleDto
            {
                ClassroomId = updateDto.ClassroomId,
                GroupId = updateDto.GroupId,
                StartTime = updateDto.StartTime,
                EndTime = updateDto.EndTime,
                DayOfWeek = updateDto.DayOfWeek,
                StartDate = updateDto.StartDate,
                EndDate = updateDto.EndDate,
                IsRecurring = updateDto.IsRecurring,
                Notes = updateDto.Notes
            };

            var conflicts = await _context.Schedules
                .Where(s => s.ClassroomId == updateDto.ClassroomId &&
                           s.Id != updateDto.Id &&
                           s.Status == ActiveStatus.Active &&
                           !s.IsDeleted &&
                           s.DayOfWeek == updateDto.DayOfWeek &&
                           s.StartDate <= updateDto.StartDate &&
                           (s.EndDate == null || s.EndDate >= updateDto.StartDate) &&
                           s.StartTime < updateDto.EndTime && s.EndTime > updateDto.StartTime)
                .AnyAsync();

            if (conflicts)
            {
                return new Response<GetScheduleDto>
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = "Вақти дарс бо дарсҳои дигар мутобиқат дорад"
                };
            }

            // Update schedule properties
            schedule.ClassroomId = updateDto.ClassroomId;
            schedule.GroupId = updateDto.GroupId;
            schedule.StartTime = updateDto.StartTime;
            schedule.EndTime = updateDto.EndTime;
            schedule.DayOfWeek = updateDto.DayOfWeek;
            schedule.StartDate = updateDto.StartDate;
            schedule.EndDate = updateDto.EndDate;
            schedule.IsRecurring = updateDto.IsRecurring;
            schedule.Notes = updateDto.Notes;
            schedule.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            return await GetScheduleByIdAsync(schedule.Id);
        }
        catch (Exception ex)
        {
            return new Response<GetScheduleDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми навсозии ҷадвали дарс: {ex.Message}"
            };
        }
    }

    public async Task<Response<bool>> DeleteScheduleAsync(int id)
    {
        try
        {
            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (schedule == null)
            {
                return new Response<bool>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Ҷадвали дарс ёфт нашуд"
                };
            }

            schedule.IsDeleted = true;
            schedule.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            return new Response<bool>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = true,
                Message = "Ҷадвали дарс бо муваффақият нест карда шуд"
            };
        }
        catch (Exception ex)
        {
            return new Response<bool>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми несткунии ҷадвали дарс: {ex.Message}"
            };
        }
    }

    public async Task<Response<ScheduleConflictDto>> CheckScheduleConflictAsync(CreateScheduleDto scheduleDto)
    {
        try
        {
            var conflictDto = new ScheduleConflictDto();

            var conflicts = await _context.Schedules
                .Include(s => s.Classroom)
                .Include(s => s.Group)
                .Where(s => s.ClassroomId == scheduleDto.ClassroomId &&
                           s.Status == ActiveStatus.Active &&
                           !s.IsDeleted &&
                           s.DayOfWeek == scheduleDto.DayOfWeek &&
                           s.StartDate <= scheduleDto.StartDate &&
                           (s.EndDate == null || s.EndDate >= scheduleDto.StartDate) &&
                           s.StartTime < scheduleDto.EndTime && s.EndTime > scheduleDto.StartTime)
                .ToListAsync();

            if (conflicts.Any())
            {
                conflictDto.HasConflict = true;
                conflictDto.Conflicts = conflicts.Select(c => new ConflictDetail
                {
                    ScheduleId = c.Id,
                    ClassroomName = c.Classroom.Name,
                    GroupName = c.Group?.Name,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime,
                    DayOfWeek = c.DayOfWeek,
                    Message = $"Вақти дарс бо {c.Group?.Name ?? "дарси дигар"} дар синфхонаи {c.Classroom.Name} мутобиқат дорад"
                }).ToList();
            }

            // Get suggestions for available time slots
            if (conflictDto.HasConflict)
            {
                var suggestionsResponse = await GetAvailableTimeSlotsAsync(
                    scheduleDto.ClassroomId,
                    scheduleDto.DayOfWeek,
                    scheduleDto.StartDate,
                    scheduleDto.EndTime - scheduleDto.StartTime);

                if (suggestionsResponse.StatusCode == 200 && suggestionsResponse.Data != null)
                {
                    conflictDto.Suggestions = suggestionsResponse.Data;
                }
            }

            return new Response<ScheduleConflictDto>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = conflictDto
            };
        }
        catch (Exception ex)
        {
            return new Response<ScheduleConflictDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми санҷиши вақти бархӯрд: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<TimeSlotSuggestion>>> GetAvailableTimeSlotsAsync(int classroomId, DayOfWeek dayOfWeek, DateOnly date, TimeSpan duration)
    {
        try
        {
            var classroom = await _context.Classrooms
                .FirstOrDefaultAsync(c => c.Id == classroomId && !c.IsDeleted);

            if (classroom == null)
            {
                return new Response<List<TimeSlotSuggestion>>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Синфхона ёфт нашуд"
                };
            }

            // Get occupied time slots for the day
            var occupiedSlots = await _context.Schedules
                .Where(s => s.ClassroomId == classroomId &&
                           s.DayOfWeek == dayOfWeek &&
                           s.Status == ActiveStatus.Active &&
                           !s.IsDeleted &&
                           s.StartDate <= date &&
                           (s.EndDate == null || s.EndDate >= date))
                .OrderBy(s => s.StartTime)
                .Select(s => new { s.StartTime, s.EndTime })
                .ToListAsync();

            var suggestions = new List<TimeSlotSuggestion>();

            // Working hours (8:00 AM to 8:00 PM)
            var workingStart = new TimeOnly(8, 0);
            var workingEnd = new TimeOnly(20, 0);

            // Generate available slots
            var currentTime = workingStart;
            while (currentTime.Add(duration) <= workingEnd)
            {
                var proposedEnd = currentTime.Add(duration);

                // Check if this slot conflicts with any occupied slot
                bool hasConflict = occupiedSlots.Any(slot =>
                    currentTime < slot.EndTime && proposedEnd > slot.StartTime);

                if (!hasConflict)
                {
                    suggestions.Add(new TimeSlotSuggestion
                    {
                        ClassroomId = classroomId,
                        ClassroomName = classroom.Name,
                        StartTime = currentTime,
                        EndTime = proposedEnd,
                        DayOfWeek = dayOfWeek,
                        IsPreferred = true
                    });
                }

                currentTime = currentTime.AddMinutes(30); // 30-minute intervals
            }

            return new Response<List<TimeSlotSuggestion>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = suggestions
            };
        }
        catch (Exception ex)
        {
            return new Response<List<TimeSlotSuggestion>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани вақтҳои холӣ: {ex.Message}"
            };
        }
    }

    public async Task<Response<bool>> IsTimeSlotAvailableAsync(int classroomId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, DateOnly date)
    {
        try
        {
            var hasConflict = await _context.Schedules
                .AnyAsync(s => s.ClassroomId == classroomId &&
                              s.DayOfWeek == dayOfWeek &&
                              s.Status == ActiveStatus.Active &&
                              !s.IsDeleted &&
                              s.StartDate <= date &&
                              (s.EndDate == null || s.EndDate >= date) &&
                              s.StartTime < endTime && s.EndTime > startTime);

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
                Message = $"Хатогӣ ҳангоми санҷиши дастрасии вақт: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetScheduleDto>>> GetWeeklyScheduleAsync(int centerId, DateOnly weekStart)
    {
        try
        {
            var weekEnd = weekStart.AddDays(6);

            var schedules = await _context.Schedules
                .Include(s => s.Classroom)
                .ThenInclude(c => c.Center)
                .Include(s => s.Group)
                .Where(s => s.Classroom.CenterId == centerId &&
                           s.Status == ActiveStatus.Active &&
                           !s.IsDeleted &&
                           s.StartDate <= weekEnd &&
                           (s.EndDate == null || s.EndDate >= weekStart))
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            var scheduleDtos = schedules.Select(MapToGetScheduleDto).ToList();

            return new Response<List<GetScheduleDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = scheduleDtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetScheduleDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани ҷадвали ҳафтавӣ: {ex.Message}"
            };
        }
    }

    private GetScheduleDto MapToGetScheduleDto(Schedule schedule)
    {
        return new GetScheduleDto
        {
            Id = schedule.Id,
            ClassroomId = schedule.ClassroomId,
            Classroom = new GetClassroomDto
            {
                Id = schedule.Classroom.Id,
                Name = schedule.Classroom.Name,
                Description = schedule.Classroom.Description,
                Capacity = schedule.Classroom.Capacity,
                IsActive = schedule.Classroom.IsActive,
                CenterId = schedule.Classroom.CenterId,
                Center = new GetCenterSimpleDto
                {
                    Id = schedule.Classroom.Center.Id,
                    Name = schedule.Classroom.Center.Name
                },
                CreatedAt = schedule.Classroom.CreatedAt,
                UpdatedAt = schedule.Classroom.UpdatedAt
            },
            GroupId = schedule.GroupId,
            Group = schedule.Group != null ? new GetGroupDto
            {
                Id = schedule.Group.Id,
                Name = schedule.Group.Name,
                Description = schedule.Group.Description
            } : null,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            DayOfWeek = schedule.DayOfWeek,
            StartDate = schedule.StartDate,
            EndDate = schedule.EndDate,
            IsRecurring = schedule.IsRecurring,
            Status = schedule.Status,
            Notes = schedule.Notes,
            CreatedAt = schedule.CreatedAt,
            UpdatedAt = schedule.UpdatedAt
        };
    }
} 