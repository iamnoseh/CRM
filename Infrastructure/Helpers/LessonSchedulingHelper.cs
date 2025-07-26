using Domain.DTOs.Group;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Infrastructure.Helpers;

public static class LessonSchedulingHelper
{
    public static async Task<Response<List<Lesson>>> GenerateSchedulesAndLessonsAsync(
        DataContext context,
        Group group,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        List<int> lessonDays,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        try
        {
            if (!group.ClassroomId.HasValue)
            {
                return new Response<List<Lesson>>(
                    HttpStatusCode.BadRequest,
                    "Синфхона интихоб нашудааст");
            }

            var lessons = new List<Lesson>();
            var schedules = new List<Schedule>();

            // Convert int days to DayOfWeek
            var dayOfWeeks = lessonDays.Select(d => (DayOfWeek)d).ToList();

            // Generate lessons for each week
            var currentDate = startDate.UtcDateTime;
            var endDateValue = endDate.UtcDateTime;
            var weekNumber = 1;

            while (currentDate <= endDateValue)
            {
                foreach (var day in dayOfWeeks)
                {
                    var lessonDate = GetDateForDayOfWeek(currentDate, day);
                    
                    if (lessonDate.Date > endDateValue.Date)
                        continue;

                    if (lessonDate.Date < currentDate.Date)
                    {
                        lessonDate = lessonDate.AddDays(7);
                        if (lessonDate.Date > endDateValue.Date)
                            continue;
                    }

                    // Check for classroom conflicts
                    var hasConflict = await CheckClassroomConflictAsync(
                        context, 
                        group.ClassroomId.Value,
                        lessonDate,
                        startTime.ToTimeSpan(),
                        endTime.ToTimeSpan());

                    if (hasConflict)
                    {
                        var availableSlots = await GetAvailableTimeSlotsAsync(
                            context,
                            group.ClassroomId.Value,
                            lessonDate);

                        return new Response<List<Lesson>>(
                            HttpStatusCode.Conflict,
                            $"Синфхона дар санаи {lessonDate:dd.MM.yyyy} аз соати {startTime} то {endTime} банд аст. Вақтҳои холӣ: {string.Join(", ", availableSlots)}");
                    }

                    // Create schedule for this time slot
                    var schedule = new Schedule
                    {
                        GroupId = group.Id,
                        ClassroomId = group.ClassroomId.Value,
                        StartTime = startTime,
                        EndTime = endTime,
                        DayOfWeek = day,
                        StartDate = DateOnly.FromDateTime(lessonDate),
                        EndDate = DateOnly.FromDateTime(lessonDate),
                        IsRecurring = false,
                        Status = ActiveStatus.Active
                    };
                    schedules.Add(schedule);

                    // Create lesson with UTC times
                    var lessonStartTime = new DateTime(
                        lessonDate.Year, lessonDate.Month, lessonDate.Day,
                        startTime.Hour, startTime.Minute, 0, DateTimeKind.Utc);
                        
                    var lessonEndTime = new DateTime(
                        lessonDate.Year, lessonDate.Month, lessonDate.Day,
                        endTime.Hour, endTime.Minute, 0, DateTimeKind.Utc);

                    var lesson = new Lesson
                    {
                        GroupId = group.Id,
                        ClassroomId = group.ClassroomId.Value,
                        StartTime = new DateTimeOffset(lessonStartTime),
                        EndTime = new DateTimeOffset(lessonEndTime),
                        DayIndex = (int)day,
                        WeekIndex = weekNumber,
                        Schedule = schedule
                    };
                    lessons.Add(lesson);
                }

                currentDate = currentDate.AddDays(7);
                weekNumber++;
            }

            await context.Schedules.AddRangeAsync(schedules);
            await context.Lessons.AddRangeAsync(lessons);
            await context.SaveChangesAsync();

            return new Response<List<Lesson>>(lessons);
        }
        catch (Exception ex)
        {
            return new Response<List<Lesson>>(
                HttpStatusCode.InternalServerError,
                $"Хатогӣ ҳангоми эҷоди дарсҳо: {ex.Message}");
        }
    }

    private static DateTime GetDateForDayOfWeek(DateTime startOfWeek, DayOfWeek targetDay)
    {
        int daysToAdd = ((int)targetDay - (int)startOfWeek.DayOfWeek + 7) % 7;
        return startOfWeek.AddDays(daysToAdd);
    }

    private static async Task<bool> CheckClassroomConflictAsync(
        DataContext context,
        int classroomId,
        DateTime date,
        TimeSpan startTime,
        TimeSpan endTime)
    {
        var startDateTime = date.Add(startTime);
        var endDateTime = date.Add(endTime);

        return await context.Lessons
            .AnyAsync(l => 
                l.ClassroomId == classroomId &&
                l.StartTime.UtcDateTime.Date == date.Date &&
                ((l.StartTime.UtcDateTime <= startDateTime && l.EndTime.UtcDateTime > startDateTime) ||
                 (l.StartTime.UtcDateTime < endDateTime && l.EndTime.UtcDateTime >= endDateTime) ||
                 (l.StartTime.UtcDateTime >= startDateTime && l.EndTime.UtcDateTime <= endDateTime)));
    }

    private static async Task<List<string>> GetAvailableTimeSlotsAsync(
        DataContext context,
        int classroomId,
        DateTime date)
    {
        var busySlots = await context.Lessons
            .Where(l => 
                l.ClassroomId == classroomId && 
                l.StartTime.UtcDateTime.Date == date.Date)
            .OrderBy(l => l.StartTime)
            .Select(l => new { l.StartTime, l.EndTime })
            .ToListAsync();

        var availableSlots = new List<string>();
        var currentTime = new TimeOnly(8, 0); // Start at 8 AM
        var endOfDay = new TimeOnly(22, 0);   // End at 10 PM

        foreach (var slot in busySlots)
        {
            var slotStart = TimeOnly.FromDateTime(slot.StartTime.UtcDateTime);
            if (currentTime < slotStart)
            {
                availableSlots.Add($"{currentTime:HH:mm}-{slotStart:HH:mm}");
            }
            currentTime = TimeOnly.FromDateTime(slot.EndTime.UtcDateTime);
        }

        if (currentTime < endOfDay)
        {
            availableSlots.Add($"{currentTime:HH:mm}-{endOfDay:HH:mm}");
        }

        return availableSlots;
    }
} 