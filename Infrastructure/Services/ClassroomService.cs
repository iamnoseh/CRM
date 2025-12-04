using System.Net;
using Domain.DTOs.Center;
using Domain.DTOs.Classroom;
using Domain.DTOs.Group;
using Domain.DTOs.Schedule;
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

public class ClassroomService(DataContext context, IHttpContextAccessor httpContextAccessor)
    : IClassroomService
{
    public async Task<Response<GetClassroomDto>> CreateClassroomAsync(CreateClassroomDto createDto)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
            {
                return new Response<GetClassroomDto>(HttpStatusCode.BadRequest, "CenterId  ёфт нашуд");
            }

            var centerExists = await context.Centers.AnyAsync(c => c.Id == centerId && !c.IsDeleted);
            if (!centerExists)
            {
                return new Response<GetClassroomDto>(HttpStatusCode.NotFound, "Маркази таълимӣ ёфт нашуд");
            }
            
            var existingClassroom = await context.Classrooms
                .AnyAsync(c => c.CenterId == centerId && 
                              c.Name.ToLower() == createDto.Name.ToLower() && 
                              !c.IsDeleted);

            if (existingClassroom)
            {
                return new Response<GetClassroomDto>(HttpStatusCode.BadRequest, "Синфхона бо ҳамин ном дар ин маркази таълимӣ аллакай мавҷуд аст");
            }

            var classroom = new Classroom
            {
                Name = createDto.Name,
                Description = createDto.Description,
                Capacity = createDto.Capacity,
                CenterId = centerId.Value,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            context.Classrooms.Add(classroom);
            await context.SaveChangesAsync();

            return await GetClassroomByIdAsync(classroom.Id);
        }
        catch (Exception ex)
        {
            return new Response<GetClassroomDto>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми сохтани синфхона: {ex.Message}");
        }
    }

    public async Task<PaginationResponse<List<GetClassroomDto>>> GetAllClassrooms(ClassroomFilter filter)
    {
        try
        {
            var query = context.Classrooms
                .Include(c => c.Center)
                .Where(c => !c.IsDeleted)
                .AsQueryable();

            query = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(query, httpContextAccessor, c => c.CenterId);

            if (!string.IsNullOrEmpty(filter.Name))
            {
                query = query.Where(c => c.Name.ToLower().Contains(filter.Name.ToLower()));
            }

            if (filter.Capacity.HasValue)
            {
                query = query.Where(c => c.Capacity == filter.Capacity);
            }

            var totalRecords = await query.CountAsync();
            var skip = (filter.PageNumber - 1) * filter.PageSize;
            
            var classrooms = await query
                .OrderBy(c => c.Name)
                .Skip(skip)
                .Take(filter.PageSize)
                .ToListAsync();

            var classroomDtos = classrooms.Select(classroom => new GetClassroomDto
            {
                Id = classroom.Id,
                Name = classroom.Name,
                Description = classroom.Description,
                Capacity = classroom.Capacity,
                IsActive = classroom.IsActive,
                CenterId = classroom.CenterId,
                Center = new GetCenterSimpleDto
                {
                    Id = classroom.Center.Id,
                    Name = classroom.Center.Name,
                },
                CreatedAt = classroom.CreatedAt,
                UpdatedAt = classroom.UpdatedAt
            }).ToList();

            return new PaginationResponse<List<GetClassroomDto>>(classroomDtos, totalRecords, filter.PageNumber, filter.PageSize)
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetClassroomDto>>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми гирифтани синфхонаҳо: {ex.Message}");
        }
    }

    public async Task<Response<GetClassroomDto>> GetClassroomByIdAsync(int id)
    {
        try
        {
            var classroom = await context.Classrooms
                .Include(c => c.Center)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (classroom == null)
            {
                return new Response<GetClassroomDto>(HttpStatusCode.NotFound, "Синфхона ёфт нашуд");
            }

            var classroomDto = new GetClassroomDto
            {
                Id = classroom.Id,
                Name = classroom.Name,
                Description = classroom.Description,
                Capacity = classroom.Capacity,
                IsActive = classroom.IsActive,
                CenterId = classroom.CenterId,
                Center = new GetCenterSimpleDto
                {
                    Id = classroom.Center.Id,
                    Name = classroom.Center.Name,
                },
                CreatedAt = classroom.CreatedAt,
                UpdatedAt = classroom.UpdatedAt
            };

            return new Response<GetClassroomDto>(classroomDto);
        }
        catch (Exception ex)
        {
            return new Response<GetClassroomDto>
            {
                Message = $"Хатогӣ ҳангоми гирифтани синфхона: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetClassroomDto>>> GetClassroomsByCenterAsync(int centerId)
    {
        try
        {
            var classrooms = await context.Classrooms
                .Include(c => c.Center)
                .Where(c => c.CenterId == centerId && !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var classroomDtos = classrooms.Select(classroom => new GetClassroomDto
            {
                Id = classroom.Id,
                Name = classroom.Name,
                Description = classroom.Description,
                Capacity = classroom.Capacity,
                IsActive = classroom.IsActive,
                CenterId = classroom.CenterId,
                Center = new GetCenterSimpleDto
                {
                    Id = classroom.Center.Id,
                    Name = classroom.Center.Name,
                },
                CreatedAt = classroom.CreatedAt,
                UpdatedAt = classroom.UpdatedAt
            }).ToList();

            return new Response<List<GetClassroomDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = classroomDtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetClassroomDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани синфхонаҳои маркази таълимӣ: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetClassroomDto>> UpdateClassroomAsync(UpdateClassroomDto updateDto)
    {
        try
        {
            var classroom = await context.Classrooms
                .FirstOrDefaultAsync(c => c.Id == updateDto.Id && !c.IsDeleted);

            if (classroom == null)
            {
                return new Response<GetClassroomDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Синфхона ёфт нашуд"
                };
            }
            var existingClassroom = await context.Classrooms
                .AnyAsync(c => c.CenterId == classroom.CenterId && 
                              c.Name.ToLower() == updateDto.Name.ToLower() && 
                              c.Id != updateDto.Id && 
                              !c.IsDeleted);

            if (existingClassroom)
            {
                return new Response<GetClassroomDto>
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Синфхона бо ҳамин ном дар ин маркази таълимӣ аллакай мавҷуд аст"
                };
            }

            classroom.Name = updateDto.Name;
            classroom.Description = updateDto.Description;
            classroom.Capacity = updateDto.Capacity;
            classroom.IsActive = updateDto.IsActive;
            classroom.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();

            return await GetClassroomByIdAsync(classroom.Id);
        }
        catch (Exception ex)
        {
            return new Response<GetClassroomDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми навсозии синфхона: {ex.Message}"
            };
        }
    }

    public async Task<Response<bool>> DeleteClassroomAsync(int id)
    {
        try
        {
            var classroom = await context.Classrooms
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (classroom == null)
            {
                return new Response<bool>(HttpStatusCode.NotFound, "Синфхона ёфт нашуд");
            }

            var hasActiveSchedules = await context.Schedules
                .AnyAsync(s => s.ClassroomId == id && 
                              s.Status == ActiveStatus.Active && 
                              !s.IsDeleted);

            if (hasActiveSchedules)
            {
                return new Response<bool>
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = "Синфхонаро нест кардан мумкин нест, зеро он дарсҳои фаъол дорад"
                };
            }

            classroom.IsDeleted = true;
            classroom.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();

            return new Response<bool>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = true,
                Message = "Синфхона бо муваффақият нест карда шуд"
            };
        }
        catch (Exception ex)
        {
            return new Response<bool>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми несткунии синфхона: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetClassroomScheduleDto>> GetClassroomScheduleAsync(int classroomId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        try
        {
            var classroom = await context.Classrooms
                .Include(c => c.Center)
                .FirstOrDefaultAsync(c => c.Id == classroomId && !c.IsDeleted);

            if (classroom == null)
            {
                return new Response<GetClassroomScheduleDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Синфхона ёфт нашуд"
                };
            }

            startDate ??= DateOnly.FromDateTime(DateTime.UtcNow.Date);
            endDate ??= startDate.Value.AddDays(6); // Week view

            var schedules = await context.Schedules
                .Include(s => s.Group)
                .Where(s => s.ClassroomId == classroomId && 
                           s.Status == ActiveStatus.Active && 
                           !s.IsDeleted &&
                           (s.StartDate <= endDate && (s.EndDate == null || s.EndDate >= startDate)))
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            var scheduleDtos = schedules.Select(s => new GetScheduleSimpleDto
            {
                Id = s.Id,
                GroupName = s.Group?.Name ?? "Номаълум",
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                DayOfWeek = s.DayOfWeek,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                IsRecurring = s.IsRecurring,
                Status = s.Status
            }).ToList();

            var result = new GetClassroomScheduleDto
            {
                ClassroomId = classroom.Id,
                ClassroomName = classroom.Name,
                CenterName = classroom.Center.Name,
                Schedules = scheduleDtos,
                AvailableTimeSlots = GenerateAvailableTimeSlots(scheduleDtos, startDate.Value, endDate.Value)
            };

            return new Response<GetClassroomScheduleDto>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = result
            };
        }
        catch (Exception ex)
        {
            return new Response<GetClassroomScheduleDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани ҷадвали синфхона: {ex.Message}"
            };
        }
    }

    public async Task<Response<ScheduleConflictDto>> CheckScheduleConflictAsync(CreateScheduleDto scheduleDto)
    {
        try
        {
            var conflictDto = new ScheduleConflictDto();

            var conflicts = await context.Schedules
                .Include(s => s.Classroom)
                .Include(s => s.Group)
                .Where(s => s.ClassroomId == scheduleDto.ClassroomId &&
                           s.Status == ActiveStatus.Active &&
                           !s.IsDeleted &&
                           s.DayOfWeek == scheduleDto.DayOfWeek &&
                           s.StartDate <= scheduleDto.StartDate &&
                           (s.EndDate == null || s.EndDate >= scheduleDto.StartDate) &&
                           ((s.StartTime < scheduleDto.EndTime && s.EndTime > scheduleDto.StartTime)))
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

            return new Response<ScheduleConflictDto>(conflictDto);
        }
        catch (Exception ex)
        {
            return new Response<ScheduleConflictDto>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми санҷиши вақти бархӯрд: {ex.Message}");
        }
    }

    public async Task<Response<GetScheduleDto>> CreateScheduleAsync(CreateScheduleDto createDto)
    {
        try
        {
            // Check for conflicts first
            var conflictCheck = await CheckScheduleConflictAsync(createDto);
            if (conflictCheck.StatusCode != 200)
            {
                return new Response<GetScheduleDto>(HttpStatusCode.InternalServerError, conflictCheck.Message);
            }

            if (conflictCheck.Data?.HasConflict == true)
            {
                return new Response<GetScheduleDto>(HttpStatusCode.Conflict, "Вақти дарс бо дарсҳои дигар мутобиқат дорад");
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

            context.Schedules.Add(schedule);
            await context.SaveChangesAsync();

            // Return the created schedule
            var createdSchedule = await context.Schedules
                .Include(s => s.Classroom)
                .ThenInclude(c => c.Center)
                .Include(s => s.Group)
                .FirstAsync(s => s.Id == schedule.Id);

            var scheduleDto = new GetScheduleDto
            {
                Id = createdSchedule.Id,
                ClassroomId = createdSchedule.ClassroomId,
                Classroom = new GetClassroomDto
                {
                    Id = createdSchedule.Classroom.Id,
                    Name = createdSchedule.Classroom.Name,
                    Description = createdSchedule.Classroom.Description,
                    Capacity = createdSchedule.Classroom.Capacity,
                    IsActive = createdSchedule.Classroom.IsActive,
                    CenterId = createdSchedule.Classroom.CenterId,
                    Center = new GetCenterSimpleDto
                    {
                        Id = createdSchedule.Classroom.Center.Id,
                        Name = createdSchedule.Classroom.Center.Name,
                    },
                    CreatedAt = createdSchedule.Classroom.CreatedAt,
                    UpdatedAt = createdSchedule.Classroom.UpdatedAt
                },
                GroupId = createdSchedule.GroupId,
                Group = createdSchedule.Group != null ? new GetGroupDto
                {
                    Id = createdSchedule.Group.Id,
                    Name = createdSchedule.Group.Name,
                    Description = createdSchedule.Group.Description
                } : null,
                StartTime = createdSchedule.StartTime,
                EndTime = createdSchedule.EndTime,
                DayOfWeek = createdSchedule.DayOfWeek,
                StartDate = createdSchedule.StartDate,
                EndDate = createdSchedule.EndDate,
                IsRecurring = createdSchedule.IsRecurring,
                Status = createdSchedule.Status,
                Notes = createdSchedule.Notes,
                CreatedAt = createdSchedule.CreatedAt,
                UpdatedAt = createdSchedule.UpdatedAt
            };

            var response = new Response<GetScheduleDto>(scheduleDto);
            response.Message = "Ҷадвали дарс бо муваффақият сохта шуд";
            return response;
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

    public async Task<Response<List<TimeSlotSuggestion>>> GetAvailableTimeSlotsAsync(int classroomId, DayOfWeek dayOfWeek, DateOnly date, TimeSpan duration)
    {
        try
        {
            var classroom = await context.Classrooms
                .FirstOrDefaultAsync(c => c.Id == classroomId && !c.IsDeleted);

            if (classroom == null)
            {
                return new Response<List<TimeSlotSuggestion>>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Синфхона ёфт нашуд"
                };
            }

            var occupiedSlots = await context.Schedules
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

            var workingStart = new TimeOnly(8, 0);
            var workingEnd = new TimeOnly(20, 0);
            var slotDuration = TimeOnly.FromTimeSpan(duration);

            var currentTime = workingStart;
            while (currentTime.Add(duration) <= workingEnd)
            {
                var proposedEnd = currentTime.Add(duration);
                
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

                currentTime = currentTime.AddMinutes(30); 
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

    public async Task<Response<List<GetClassroomDto>>> GetAvailableClassroomsAsync(int centerId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, DateOnly date)
    {
        try
        {
            var allClassrooms = await context.Classrooms
                .Include(c => c.Center)
                .Where(c => c.CenterId == centerId && c.IsActive && !c.IsDeleted)
                .ToListAsync();
            var conflictedClassroomIds = await context.Schedules
                .Where(s => allClassrooms.Select(c => c.Id).Contains(s.ClassroomId) &&
                           s.DayOfWeek == dayOfWeek &&
                           s.Status == ActiveStatus.Active &&
                           !s.IsDeleted &&
                           s.StartDate <= date &&
                           (s.EndDate == null || s.EndDate >= date) &&
                           s.StartTime < endTime && s.EndTime > startTime)
                .Select(s => s.ClassroomId)
                .ToListAsync();

            var availableClassrooms = allClassrooms
                .Where(c => !conflictedClassroomIds.Contains(c.Id))
                .Select(c => new GetClassroomDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Capacity = c.Capacity,
                    IsActive = c.IsActive,
                    CenterId = c.CenterId,
                    Center = new GetCenterSimpleDto
                    {
                        Id = c.Center.Id,
                        Name = c.Center.Name,
                    },
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .OrderBy(c => c.Name)
                .ToList();

            return new Response<List<GetClassroomDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = availableClassrooms
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetClassroomDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Хатогӣ ҳангоми гирифтани синфхонаҳои холӣ: {ex.Message}"
            };
        }
    }

    private List<TimeSlotDto> GenerateAvailableTimeSlots(
        List<GetScheduleSimpleDto> schedules, 
        DateOnly startDate, 
        DateOnly endDate)
    {
        var timeSlots = new List<TimeSlotDto>();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            var dayOfWeek = (DayOfWeek)currentDate.DayOfWeek;
            var daySchedules = schedules
                .Where(s => s.DayOfWeek == dayOfWeek &&
                           s.StartDate <= currentDate &&
                           (s.EndDate == null || s.EndDate >= currentDate))
                .OrderBy(s => s.StartTime)
                .ToList();

            var currentTime = new TimeOnly(8, 0); 
            var endOfDay = new TimeOnly(22, 0); 

            foreach (var schedule in daySchedules)
            {
                if (currentTime < schedule.StartTime)
                {
                    timeSlots.Add(new TimeSlotDto
                    {
                        DayOfWeek = dayOfWeek,
                        StartTime = currentTime,
                        EndTime = schedule.StartTime,
                        IsAvailable = true
                    });
                }

                timeSlots.Add(new TimeSlotDto
                {
                    DayOfWeek = dayOfWeek,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    IsAvailable = false,
                    OccupiedBy = schedule.GroupName
                });

                currentTime = schedule.EndTime;
            }

            if (currentTime < endOfDay)
            {
                timeSlots.Add(new TimeSlotDto
                {
                    DayOfWeek = dayOfWeek,
                    StartTime = currentTime,
                    EndTime = endOfDay,
                    IsAvailable = true
                });
            }

            currentDate = currentDate.AddDays(1);
        }

        return timeSlots;
    }

    public async Task<PaginationResponse<List<GetSimpleClassroomDto>>> GetSimpleClassrooms(BaseFilter filter)
    {
        try
        {
            var classroomsQuery = context.Classrooms
                .Include(c => c.Center)
                .Where(c => !c.IsDeleted)
                .AsQueryable();
            
            classroomsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
                classroomsQuery, httpContextAccessor, c => c.CenterId);

            var totalRecords = await classroomsQuery.CountAsync();

            var skip = (filter.PageNumber - 1) * filter.PageSize;
            var classrooms = await classroomsQuery
                .OrderBy(c => c.Name)
                .Skip(skip)
                .Take(filter.PageSize)
                .Select(c => new GetSimpleClassroomDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            return new PaginationResponse<List<GetSimpleClassroomDto>>(
                classrooms,
                totalRecords,
                filter.PageNumber,
                filter.PageSize);
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetSimpleClassroomDto>>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми гирифтани синфхонаҳо: {ex.Message}");
        }
    }
} 