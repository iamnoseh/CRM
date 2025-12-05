using System.Net;
using Domain.DTOs.Schedule;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Constants;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ScheduleService(DataContext context, IHttpContextAccessor httpContextAccessor)
    : IScheduleService
{
    #region CreateScheduleAsync

    public async Task<Response<GetScheduleDto>> CreateScheduleAsync(CreateScheduleDto dto)
    {
        try
        {
            var access = await EnsureAccessAsync(dto.ClassroomId, dto.GroupId);
            if (access.StatusCode != (int)HttpStatusCode.OK)
                return new Response<GetScheduleDto>((HttpStatusCode)access.StatusCode, access.Message ?? Messages.Common.AccessDenied);

            var conflictCheck = await CheckScheduleConflictAsync(dto);
            if (conflictCheck.StatusCode != (int)HttpStatusCode.OK)
                return new Response<GetScheduleDto>((HttpStatusCode)conflictCheck.StatusCode, conflictCheck.Message ?? Messages.Schedule.ConflictCheckError);

            if (conflictCheck.Data.HasConflict)
                return new Response<GetScheduleDto>(HttpStatusCode.Conflict, Messages.Schedule.ConflictDetected);

            var schedule = new Schedule
            {
                ClassroomId = dto.ClassroomId,
                GroupId = dto.GroupId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                DayOfWeek = dto.DayOfWeek,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsRecurring = dto.IsRecurring,
                Notes = dto.Notes,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            context.Schedules.Add(schedule);
            await context.SaveChangesAsync();
            return await GetScheduleByIdAsync(schedule.Id);
        }
        catch (Exception ex)
        {
            return new Response<GetScheduleDto>(HttpStatusCode.InternalServerError, string.Format(Messages.Schedule.CreateError, ex.Message));
        }
    }

    #endregion

    #region GetScheduleByIdAsync

    public async Task<Response<GetScheduleDto>> GetScheduleByIdAsync(int id)
    {
        try
        {
            var schedule = await context.Schedules
                .Include(s => s.Classroom)
                    .ThenInclude(c => c!.Center)
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (schedule == null)
                return new Response<GetScheduleDto>(HttpStatusCode.NotFound, Messages.Schedule.NotFound);

            var userCenter = CurrentCenterId;
            if (userCenter != null && schedule.Classroom!.CenterId != userCenter)
                return new Response<GetScheduleDto>(HttpStatusCode.Forbidden, Messages.Common.AccessDenied);

            return new Response<GetScheduleDto>(DtoMappingHelper.MapToGetScheduleDto(schedule));
        }
        catch (Exception ex)
        {
            return new Response<GetScheduleDto>(HttpStatusCode.InternalServerError, string.Format(Messages.Schedule.FetchError, ex.Message));
        }
    }

    #endregion

    #region GetSchedulesByClassroomAsync

    public async Task<Response<List<GetScheduleDto>>> GetSchedulesByClassroomAsync(int classroomId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        try
        {
            if (!await IsClassroomAllowedAsync(classroomId))
                return new Response<List<GetScheduleDto>>(HttpStatusCode.Forbidden, Messages.Schedule.AccessDeniedClassroom);

            var from = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var to = endDate ?? from.AddDays(6);

            var schedules = await context.Schedules
                .Include(s => s.Classroom)
                    .ThenInclude(c => c!.Center)
                .Include(s => s.Group)
                .Where(s => s.ClassroomId == classroomId &&
                            s.Status == ActiveStatus.Active &&
                            !s.IsDeleted &&
                            s.StartDate <= to &&
                            (s.EndDate == null || s.EndDate >= from))
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            return new Response<List<GetScheduleDto>>(schedules.Select(DtoMappingHelper.MapToGetScheduleDto).ToList());
        }
        catch (Exception ex)
        {
            return new Response<List<GetScheduleDto>>(HttpStatusCode.InternalServerError, string.Format(Messages.Schedule.FetchError, ex.Message));
        }
    }

    #endregion

    #region GetSchedulesByGroupAsync

    public async Task<Response<List<GetScheduleDto>>> GetSchedulesByGroupAsync(int groupId)
    {
        try
        {
            var (groupAllowed, _) = await IsGroupAllowedAsync(groupId);
            if (!groupAllowed)
                return new Response<List<GetScheduleDto>>(HttpStatusCode.Forbidden, Messages.Schedule.AccessDeniedGroup);

            var schedules = await context.Schedules
                .Include(s => s.Classroom)
                .Include(s => s.Group)
                .Where(s => s.GroupId == groupId && s.Status == ActiveStatus.Active && !s.IsDeleted)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            return new Response<List<GetScheduleDto>>(schedules.Select(DtoMappingHelper.MapToGetScheduleDto).ToList());
        }
        catch (Exception ex)
        {
            return new Response<List<GetScheduleDto>>(HttpStatusCode.InternalServerError, string.Format(Messages.Schedule.FetchError, ex.Message));
        }
    }

    #endregion

    #region UpdateScheduleAsync

    public async Task<Response<GetScheduleDto>> UpdateScheduleAsync(UpdateScheduleDto dto)
    {
        try
        {
            var schedule = await context.Schedules.FirstOrDefaultAsync(s => s.Id == dto.Id && !s.IsDeleted);
            if (schedule == null)
                return new Response<GetScheduleDto>(HttpStatusCode.NotFound, Messages.Schedule.NotFound);

            var access = await EnsureAccessAsync(dto.ClassroomId, dto.GroupId);
            if (access.StatusCode != (int)HttpStatusCode.OK)
                return new Response<GetScheduleDto>((HttpStatusCode)access.StatusCode, access.Message ?? Messages.Common.AccessDenied);

            var conflictExists = await context.Schedules
                .Where(s => s.ClassroomId == dto.ClassroomId &&
                            s.Id != dto.Id &&
                            s.Status == ActiveStatus.Active &&
                            !s.IsDeleted &&
                            s.DayOfWeek == dto.DayOfWeek &&
                            s.StartDate <= dto.StartDate &&
                            (s.EndDate == null || s.EndDate >= dto.StartDate) &&
                            s.StartTime < dto.EndTime && s.EndTime > dto.StartTime)
                .AnyAsync();

            if (conflictExists)
                return new Response<GetScheduleDto>(HttpStatusCode.Conflict, Messages.Schedule.ConflictDetected);

            schedule.ClassroomId = dto.ClassroomId;
            schedule.GroupId = dto.GroupId;
            schedule.StartTime = dto.StartTime;
            schedule.EndTime = dto.EndTime;
            schedule.DayOfWeek = dto.DayOfWeek;
            schedule.StartDate = dto.StartDate;
            schedule.EndDate = dto.EndDate;
            schedule.IsRecurring = dto.IsRecurring;
            schedule.Notes = dto.Notes;
            schedule.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();
            return await GetScheduleByIdAsync(schedule.Id);
        }
        catch (Exception ex)
        {
            return new Response<GetScheduleDto>(HttpStatusCode.InternalServerError, string.Format(Messages.Schedule.UpdateError, ex.Message));
        }
    }

    #endregion

    #region DeleteScheduleAsync

    public async Task<Response<bool>> DeleteScheduleAsync(int id)
    {
        try
        {
            var schedule = await context.Schedules.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
            if (schedule == null)
                return new Response<bool>(HttpStatusCode.NotFound, Messages.Schedule.NotFound);

            if (!await IsClassroomAllowedAsync(schedule.ClassroomId))
                return new Response<bool>(HttpStatusCode.Forbidden, Messages.Common.AccessDenied);

            schedule.IsDeleted = true;
            schedule.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();

            var response = new Response<bool>(true) { Message = Messages.Schedule.Deleted };
            return response;
        }
        catch (Exception ex)
        {
            return new Response<bool>(HttpStatusCode.InternalServerError, string.Format(Messages.Schedule.DeleteError, ex.Message));
        }
    }

    #endregion

    #region CheckScheduleConflictAsync

    public async Task<Response<ScheduleConflictDto>> CheckScheduleConflictAsync(CreateScheduleDto dto)
    {
        try
        {
            var access = await EnsureAccessAsync(dto.ClassroomId, dto.GroupId);
            if (access.StatusCode != (int)HttpStatusCode.OK)
                return new Response<ScheduleConflictDto>((HttpStatusCode)access.StatusCode, access.Message ?? Messages.Common.AccessDenied);

            var conflicts = await context.Schedules
                .Include(s => s.Classroom)
                .Include(s => s.Group)
                .Where(s => s.ClassroomId == dto.ClassroomId &&
                            s.Status == ActiveStatus.Active &&
                            !s.IsDeleted &&
                            s.DayOfWeek == dto.DayOfWeek &&
                            s.StartDate <= dto.StartDate &&
                            (s.EndDate == null || s.EndDate >= dto.StartDate) &&
                            s.StartTime < dto.EndTime && s.EndTime > dto.StartTime)
                .ToListAsync();

            var conflictDto = new ScheduleConflictDto();
            if (conflicts.Any())
            {
                conflictDto.HasConflict = true;
                conflictDto.Conflicts = conflicts.Select(c => new ConflictDetail
                {
                    ScheduleId = c.Id,
                    ClassroomName = c.Classroom?.Name ?? string.Empty,
                    GroupName = c.Group?.Name,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime,
                    DayOfWeek = c.DayOfWeek,
                    Message = Messages.Schedule.ConflictDetected
                }).ToList();

                var suggestions = await GetAvailableTimeSlotsAsync(dto.ClassroomId, dto.DayOfWeek, dto.StartDate, dto.EndTime - dto.StartTime);
                if (suggestions.StatusCode == (int)HttpStatusCode.OK)
                    conflictDto.Suggestions = suggestions.Data;
            }

            return new Response<ScheduleConflictDto>(conflictDto);
        }
        catch (Exception ex)
        {
            return new Response<ScheduleConflictDto>(HttpStatusCode.InternalServerError, string.Format(Messages.Schedule.ConflictCheckError, ex.Message));
        }
    }

    #endregion

    #region GetAvailableTimeSlotsAsync

    public async Task<Response<List<TimeSlotSuggestion>>> GetAvailableTimeSlotsAsync(int classroomId, DayOfWeek dayOfWeek, DateOnly date, TimeSpan duration)
    {
        try
        {
            if (!await IsClassroomAllowedAsync(classroomId))
                return new Response<List<TimeSlotSuggestion>>(HttpStatusCode.Forbidden, Messages.Schedule.AccessDeniedClassroom);

            var classroom = await context.Classrooms.FirstOrDefaultAsync(c => c.Id == classroomId && !c.IsDeleted);
            if (classroom == null)
                return new Response<List<TimeSlotSuggestion>>(HttpStatusCode.NotFound, Messages.Classroom.NotFound);

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
            var current = workingStart;

            while (current.Add(duration) <= workingEnd)
            {
                var proposedEnd = current.Add(duration);
                var hasConflict = occupiedSlots.Any(slot => current < slot.EndTime && proposedEnd > slot.StartTime);
                if (!hasConflict)
                {
                    suggestions.Add(new TimeSlotSuggestion
                    {
                        ClassroomId = classroomId,
                        ClassroomName = classroom.Name,
                        StartTime = current,
                        EndTime = proposedEnd,
                        DayOfWeek = dayOfWeek,
                        IsPreferred = true
                    });
                }
                current = current.AddMinutes(30);
            }

            return new Response<List<TimeSlotSuggestion>>(suggestions);
        }
        catch (Exception ex)
        {
            return new Response<List<TimeSlotSuggestion>>(HttpStatusCode.InternalServerError, string.Format(Messages.Schedule.AvailableSlotsError, ex.Message));
        }
    }

    #endregion

    #region IsTimeSlotAvailableAsync

    public async Task<Response<bool>> IsTimeSlotAvailableAsync(int classroomId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, DateOnly date)
    {
        try
        {
            if (!await IsClassroomAllowedAsync(classroomId))
                return new Response<bool>(HttpStatusCode.Forbidden, Messages.Schedule.AccessDeniedClassroom);

            var hasConflict = await context.Schedules
                .AnyAsync(s => s.ClassroomId == classroomId &&
                               s.DayOfWeek == dayOfWeek &&
                               s.Status == ActiveStatus.Active &&
                               !s.IsDeleted &&
                               s.StartDate <= date &&
                               (s.EndDate == null || s.EndDate >= date) &&
                               s.StartTime < endTime && s.EndTime > startTime);

            return new Response<bool>(!hasConflict);
        }
        catch (Exception ex)
        {
            return new Response<bool>(HttpStatusCode.InternalServerError, string.Format(Messages.Schedule.AvailableSlotsError, ex.Message));
        }
    }

    #endregion

    #region GetWeeklyScheduleAsync

    public async Task<Response<List<GetScheduleDto>>> GetWeeklyScheduleAsync(int centerId, DateOnly weekStart)
    {
        try
        {
            var userCenter = CurrentCenterId;
            if (userCenter != null && userCenter.Value != centerId)
                return new Response<List<GetScheduleDto>>(HttpStatusCode.Forbidden, Messages.Common.AccessDenied);

            var weekEnd = weekStart.AddDays(6);
            var schedules = await context.Schedules
                .Include(s => s.Classroom)
                    .ThenInclude(c => c!.Center)
                .Include(s => s.Group)
                .Where(s => s.Classroom!.CenterId == centerId &&
                            s.Status == ActiveStatus.Active &&
                            !s.IsDeleted &&
                            s.StartDate <= weekEnd &&
                            (s.EndDate == null || s.EndDate >= weekStart))
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            return new Response<List<GetScheduleDto>>(schedules.Select(DtoMappingHelper.MapToGetScheduleDto).ToList());
        }
        catch (Exception ex)
        {
            return new Response<List<GetScheduleDto>>(HttpStatusCode.InternalServerError, string.Format(Messages.Schedule.WeeklyError, ex.Message));
        }
    }

    #endregion

    #region Helpers

    private int? CurrentCenterId => UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

    private async Task<bool> IsClassroomAllowedAsync(int classroomId)
    {
        var centerId = await context.Classrooms.Where(c => c.Id == classroomId)
            .Select(c => (int?)c.CenterId)
            .FirstOrDefaultAsync();

        if (centerId == null)
            return false;

        var userCenter = CurrentCenterId;
        return userCenter == null || userCenter.Value == centerId.Value;
    }

    private async Task<(bool allowed, int? centerId)> IsGroupAllowedAsync(int? groupId)
    {
        if (!groupId.HasValue)
            return (true, null);

        var centerId = await context.Groups
            .Include(g => g.Course)
            .Where(g => g.Id == groupId.Value)
            .Select(g => (int?)g.Course!.CenterId)
            .FirstOrDefaultAsync();

        if (centerId == null)
            return (false, null);

        var userCenter = CurrentCenterId;
        var allowed = userCenter == null || userCenter.Value == centerId.Value;
        return (allowed, centerId);
    }

    private async Task<Response<bool>> EnsureAccessAsync(int classroomId, int? groupId)
    {
        if (!await IsClassroomAllowedAsync(classroomId))
            return new Response<bool>(HttpStatusCode.Forbidden, Messages.Schedule.AccessDeniedClassroom);

        var classroomCenter = await context.Classrooms.Where(c => c.Id == classroomId)
            .Select(c => (int?)c.CenterId)
            .FirstOrDefaultAsync();

        if (classroomCenter == null)
            return new Response<bool>(HttpStatusCode.NotFound, Messages.Classroom.NotFound);

        var (groupAllowed, groupCenterId) = await IsGroupAllowedAsync(groupId);
        if (!groupAllowed)
            return new Response<bool>(HttpStatusCode.Forbidden, Messages.Schedule.AccessDeniedGroup);

        if (groupCenterId.HasValue && classroomCenter != groupCenterId.Value)
            return new Response<bool>(HttpStatusCode.BadRequest, Messages.Schedule.ClassroomGroupMismatch);

        return new Response<bool>(true);
    }

    #endregion
}