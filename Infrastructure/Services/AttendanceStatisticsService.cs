using System.Net;
using Domain.DTOs.Statistics;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class AttendanceStatisticsService(DataContext context) : IAttendanceStatisticsService
{
    public async Task<Response<StudentAttendanceAllStatisticsDto>> GetStudentAttendanceStatisticsAsync(
        int studentId, 
        int? groupId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            var student = await context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);

            if (student == null)
                return new Response<StudentAttendanceAllStatisticsDto>(HttpStatusCode.NotFound, "Student not found");

            var query = context.Attendances
                .Include(a => a.Group)
                .Where(a => a.StudentId == studentId && !a.IsDeleted);

            if (groupId.HasValue)
                query = query.Where(a => a.GroupId == groupId.Value);

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.CreatedAt <= endDate.Value);

            var attendances = await query.ToListAsync();

            var statistics = new StudentAttendanceAllStatisticsDto
            {
                StudentId = studentId,
                StudentName = student.FullName,
                GroupId = groupId ?? 0,
                GroupName = groupId.HasValue ? attendances.FirstOrDefault()?.Group?.Name ?? "Unknown" : "All Groups",
                TotalLessons = attendances.Count,
                PresentCount = attendances.Count(a => a.Status == AttendanceStatus.Present),
                AbsentCount = attendances.Count(a => a.Status == AttendanceStatus.Absent),
                LateCount = attendances.Count(a => a.Status == AttendanceStatus.Late),
                StartDate = startDate ?? attendances.Min(a => a.CreatedAt),
                EndDate = endDate ?? attendances.Max(a => a.CreatedAt)
            };

            statistics.AttendancePercentage = statistics.TotalLessons > 0
                ? Math.Round((double)(statistics.PresentCount + (statistics.LateCount * 0.5)) / statistics.TotalLessons * 100, 2)
                : 0;

            return new Response<StudentAttendanceAllStatisticsDto>(statistics);
        }
        catch (Exception ex)
        {
            return new Response<StudentAttendanceAllStatisticsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<GroupAttendanceAllStatisticsDto>> GetGroupAttendanceStatisticsAsync(
        int groupId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

            if (group == null)
                return new Response<GroupAttendanceAllStatisticsDto>(HttpStatusCode.NotFound, "Group not found");

            var query = context.Attendances
                .Include(a => a.Student)
                .Where(a => a.GroupId == groupId && !a.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.CreatedAt <= endDate.Value);

            var attendances = await query.ToListAsync();

            var studentAttendances = attendances
                .GroupBy(a => new { a.StudentId, StudentName = a.Student.FullName })
                .Select(g => new StudentAttendanceAllStatisticsDto
                {
                    StudentId = g.Key.StudentId,
                    StudentName = g.Key.StudentName,
                    GroupId = groupId,
                    GroupName = group.Name,
                    TotalLessons = g.Count(),
                    PresentCount = g.Count(a => a.Status == AttendanceStatus.Present),
                    AbsentCount = g.Count(a => a.Status == AttendanceStatus.Absent),
                    LateCount = g.Count(a => a.Status == AttendanceStatus.Late),
                    AttendancePercentage = g.Count() > 0
                        ? Math.Round((double)(g.Count(a => a.Status == AttendanceStatus.Present) + 
                          (g.Count(a => a.Status == AttendanceStatus.Late) * 0.5)) / g.Count() * 100, 2)
                        : 0
                })
                .ToList();

            var statistics = new GroupAttendanceAllStatisticsDto
            {
                GroupId = groupId,
                GroupName = group.Name,
                TotalStudents = studentAttendances.Count,
                TotalLessons = attendances.Count,
                PresentCount = attendances.Count(a => a.Status == AttendanceStatus.Present),
                AbsentCount = attendances.Count(a => a.Status == AttendanceStatus.Absent),
                LateCount = attendances.Count(a => a.Status == AttendanceStatus.Late),
                StartDate = startDate ?? attendances.Min(a => a.CreatedAt),
                EndDate = endDate ?? attendances.Max(a => a.CreatedAt),
                TopStudents = studentAttendances
                    .OrderByDescending(s => s.AttendancePercentage)
                    .Take(5)
                    .ToList(),
                LowAttendanceStudents = studentAttendances
                    .OrderBy(s => s.AttendancePercentage)
                    .Take(5)
                    .ToList()
            };

            statistics.AttendancePercentage = statistics.TotalLessons > 0
                ? Math.Round((double)(statistics.PresentCount + (statistics.LateCount * 0.5)) / 
                           (statistics.TotalLessons * statistics.TotalStudents) * 100, 2)
                : 0;

            return new Response<GroupAttendanceAllStatisticsDto>(statistics);
        }
        catch (Exception ex)
        {
            return new Response<GroupAttendanceAllStatisticsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<List<GroupAttendanceAllStatisticsDto>>> GetDailyGroupAttendanceStatisticsAsync(
        int groupId,
        DateTimeOffset date)
    {
        try
        {
            var localStartDate = date.ToDushanbeTime().Date;
            var startDate = localStartDate.ToUtc();
            var endDate = startDate.AddDays(1);

            var result = await GetGroupAttendanceStatisticsAsync(groupId, startDate, endDate);
            return new Response<List<GroupAttendanceAllStatisticsDto>>(
                new List<GroupAttendanceAllStatisticsDto> { result.Data });
        }
        catch (Exception ex)
        {
            return new Response<List<GroupAttendanceAllStatisticsDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<CenterAttendanceAllStatisticsDto>> GetCenterAttendanceStatisticsAsync(
        int centerId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            var center = await context.Centers
                .FirstOrDefaultAsync(c => c.Id == centerId && !c.IsDeleted);

            if (center == null)
                return new Response<CenterAttendanceAllStatisticsDto>(HttpStatusCode.NotFound, "Center not found");

            var groups = await context.Groups
                .Where(g => g.Course.CenterId == centerId && !g.IsDeleted)
                .ToListAsync();

            var groupStatistics = new List<GroupAttendanceAllStatisticsDto>();

            foreach (var group in groups)
            {
                var groupStats = await GetGroupAttendanceStatisticsAsync(group.Id, startDate, endDate);
                if (groupStats.StatusCode == (int)HttpStatusCode.OK)
                {
                    groupStatistics.Add(groupStats.Data);
                }
            }

            var statistics = new CenterAttendanceAllStatisticsDto
            {
                CenterId = centerId,
                CenterName = center.Name,
                TotalGroups = groupStatistics.Count,
                TotalStudents = groupStatistics.Sum(g => g.TotalStudents),
                TotalLessons = groupStatistics.Sum(g => g.TotalLessons),
                PresentCount = groupStatistics.Sum(g => g.PresentCount),
                AbsentCount = groupStatistics.Sum(g => g.AbsentCount),
                LateCount = groupStatistics.Sum(g => g.LateCount),
                StartDate = startDate ?? groupStatistics.Min(g => g.StartDate),
                EndDate = endDate ?? groupStatistics.Max(g => g.EndDate),
                GroupStatistics = groupStatistics
            };

            statistics.AttendancePercentage = statistics.TotalLessons > 0
                ? Math.Round((double)(statistics.PresentCount + (statistics.LateCount * 0.5)) / 
                           statistics.TotalLessons * 100, 2)
                : 0;

            return new Response<CenterAttendanceAllStatisticsDto>(statistics);
        }
        catch (Exception ex)
        {
            return new Response<CenterAttendanceAllStatisticsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<List<CenterAttendanceAllStatisticsDto>>> GetDailyCenterAttendanceStatisticsAsync(
        int centerId,
        DateTimeOffset date)
    {
        try
        {
            var localStartDate = date.ToDushanbeTime().Date;
            var startDate = localStartDate.ToUtc();
            var endDate = startDate.AddDays(1);

            var result = await GetCenterAttendanceStatisticsAsync(centerId, startDate, endDate);
            return new Response<List<CenterAttendanceAllStatisticsDto>>(
                new List<CenterAttendanceAllStatisticsDto> { result.Data });
        }
        catch (Exception ex)
        {
            return new Response<List<CenterAttendanceAllStatisticsDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
