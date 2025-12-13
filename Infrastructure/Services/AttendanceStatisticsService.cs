using System.Net;
using Domain.DTOs.Statistics;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Constants;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Infrastructure.Services;

public class AttendanceStatisticsService(
    DataContext db,
    IHttpContextAccessor httpContextAccessor) : IAttendanceStatisticsService
{
    #region GetDailyAttendanceSummaryAsync

    public async Task<Response<DailyAttendanceSummaryDto>> GetDailyAttendanceSummaryAsync(DateTime date,
        int? centerId = null)
    {
        try
        {
            if (date == default)
                date = DateTime.Now.Date;

            var userCenterId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var user = httpContextAccessor.HttpContext?.User;
            var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value)
                .ToList();
            bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");

            var effectiveCenterId = isSuperAdmin ? centerId : userCenterId;

            var studentsWithPaidLessons = await db.Students
                .Where(s => !s.IsDeleted &&
                            s.ActiveStatus == ActiveStatus.Active &&
                            s.PaymentStatus == PaymentStatus.Paid &&
                            (effectiveCenterId == null || s.CenterId == effectiveCenterId))
                .CountAsync();

            var presentStudentsQuery = db.JournalEntries
                .Where(je => je.EntryDate.Date == date.Date &&
                             je.AttendanceStatus == AttendanceStatus.Present &&
                             !je.IsDeleted);

            if (effectiveCenterId.HasValue)
                presentStudentsQuery =
                    presentStudentsQuery.Where(je => je.Student!.CenterId == effectiveCenterId.Value);

            var presentStudents = await presentStudentsQuery
                .Select(je => je.StudentId)
                .Distinct()
                .CountAsync();

            var absentStudentsQuery = db.JournalEntries
                .Where(je => je.EntryDate.Date == date.Date &&
                             je.AttendanceStatus == AttendanceStatus.Absent &&
                             !je.IsDeleted);

            if (effectiveCenterId.HasValue)
                absentStudentsQuery = absentStudentsQuery.Where(je => je.Student!.CenterId == effectiveCenterId.Value);

            var absentStudents = await absentStudentsQuery
                .Select(je => je.StudentId)
                .Distinct()
                .CountAsync();

            var lateStudentsQuery = db.JournalEntries
                .Where(je => je.EntryDate.Date == date.Date &&
                             je.AttendanceStatus == AttendanceStatus.Late &&
                             !je.IsDeleted);

            if (effectiveCenterId.HasValue)
                lateStudentsQuery = lateStudentsQuery.Where(je => je.Student!.CenterId == effectiveCenterId.Value);

            var lateStudents = await lateStudentsQuery
                .Select(je => je.StudentId)
                .Distinct()
                .CountAsync();

            var attendanceRate = studentsWithPaidLessons > 0
                ? Math.Round((double)(presentStudents + lateStudents) / studentsWithPaidLessons * 100, 2)
                : 0;

            var result = new DailyAttendanceSummaryDto
            {
                Date = date,
                StudentsWithPaidLessons = studentsWithPaidLessons,
                PresentStudents = presentStudents,
                AbsentStudents = absentStudents,
                LateStudents = lateStudents,
                AttendanceRate = attendanceRate
            };

            return new Response<DailyAttendanceSummaryDto>
            {
                Data = result,
                StatusCode = 200,
                Message = Messages.AttendanceStatistics.DailySummarySuccess
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting daily attendance summary for date {Date}. Error: {Error}", date, ex.Message);
            return new Response<DailyAttendanceSummaryDto>(HttpStatusCode.InternalServerError,
                string.Format(Messages.AttendanceStatistics.DailySummaryError, ex.Message));
        }
    }

    #endregion

    #region GetAbsentStudentsAsync

    public async Task<Response<List<AbsentStudentDto>>> GetAbsentStudentsAsync(DateTime date, int? centerId = null)
    {
        try
        {
            if (date == default)
                date = DateTime.Now.Date;

            var userCenterId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var user = httpContextAccessor.HttpContext?.User;
            var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value)
                .ToList();
            bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");

            var effectiveCenterId = isSuperAdmin ? centerId : userCenterId;

            var absentStudents = new List<AbsentStudentDto>();

            var groupsWithLessonsToday = await db.Groups
                .Where(g => !g.IsDeleted &&
                            g.Started &&
                            g.Status == ActiveStatus.Active &&
                            g.LessonDays != null &&
                            g.LessonStartTime != null &&
                            g.LessonEndTime != null &&
                            (effectiveCenterId == null || g.Mentor!.CenterId == effectiveCenterId))
                .ToListAsync();

            foreach (var group in groupsWithLessonsToday)
            {
                if (group.LessonDays == null || group.LessonStartTime == null || group.LessonEndTime == null)
                    continue;

                var dayOfWeek = (int)date.DayOfWeek;
                var lessonDays = group.LessonDays.Split(',').Select(int.Parse).ToList();

                if (!lessonDays.Contains(dayOfWeek))
                    continue;

                var lessonStartTime = group.LessonStartTime.Value;
                var currentTime = TimeOnly.FromDateTime(DateTime.Now);

                if (currentTime >= lessonStartTime)
                {
                    var groupStudents = await db.StudentGroups
                        .Where(sg => sg.GroupId == group.Id &&
                                     sg.IsActive &&
                                     !sg.IsDeleted &&
                                     sg.Student!.PaymentStatus == PaymentStatus.Paid)
                        .Include(studentGroup => studentGroup.Student)
                        .ToListAsync();

                    foreach (var studentGroup in groupStudents)
                    {
                        var journalEntry = await db.JournalEntries
                            .Where(je => je.StudentId == studentGroup.StudentId &&
                                         je.EntryDate.Date == date.Date &&
                                         !je.IsDeleted)
                            .FirstOrDefaultAsync();

                        if (journalEntry == null || journalEntry.AttendanceStatus == AttendanceStatus.Absent)
                        {
                            var lastAttendanceDate = await db.JournalEntries
                                .Where(je => je.StudentId == studentGroup.StudentId &&
                                             je.AttendanceStatus == AttendanceStatus.Present &&
                                             !je.IsDeleted)
                                .OrderByDescending(je => je.EntryDate)
                                .Select(je => je.EntryDate)
                                .FirstOrDefaultAsync();

                            var consecutiveDays = 0;
                            if (lastAttendanceDate != default)
                            {
                                var checkDate = date.AddDays(-1);

                                while (checkDate >= lastAttendanceDate)
                                {
                                    var wasPresent = await db.JournalEntries
                                        .AnyAsync(je => je.StudentId == studentGroup.StudentId &&
                                                        je.EntryDate.Date == checkDate.Date &&
                                                        je.AttendanceStatus == AttendanceStatus.Present &&
                                                        !je.IsDeleted);

                                    if (wasPresent) break;

                                    consecutiveDays++;
                                    checkDate = checkDate.AddDays(-1);
                                }
                            }

                            absentStudents.Add(new AbsentStudentDto
                            {
                                StudentId = studentGroup.StudentId,
                                FullName = studentGroup.Student!.FullName,
                                PhoneNumber = studentGroup.Student.PhoneNumber,
                                GroupId = group.Id,
                                GroupName = group.Name,
                                LastAttendanceDate = lastAttendanceDate,
                                ConsecutiveAbsentDays = consecutiveDays
                            });
                        }
                    }
                }
            }

            return new Response<List<AbsentStudentDto>>
            {
                Data = absentStudents,
                StatusCode = 200,
                Message = string.Format(Messages.AttendanceStatistics.AbsentStudentsSuccess, absentStudents.Count)
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting absent students for date {Date}", date);
            return new Response<List<AbsentStudentDto>>(HttpStatusCode.InternalServerError,
                Messages.AttendanceStatistics.AbsentStudentsError);
        }
    }

    #endregion

    #region GetMonthlyAttendanceStatisticsAsync

    public async Task<Response<MonthlyAttendanceStatisticsDto>> GetMonthlyAttendanceStatisticsAsync(int month, int year,
        int? centerId = null)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var user = httpContextAccessor.HttpContext?.User;
            var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value)
                .ToList();
            bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");

            var effectiveCenterId = isSuperAdmin ? centerId : userCenterId;

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var dailySummaries = new List<DailyAttendanceSummaryDto>();
            var absentStudents = new List<AbsentStudentDto>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dailySummary = await GetDailyAttendanceSummaryAsync(date, effectiveCenterId);
                dailySummaries.Add(dailySummary.Data);
            }

            var monthlyAbsentStudents = await GetAbsentStudentsAsync(endDate, effectiveCenterId);
            absentStudents = monthlyAbsentStudents.Data;

            var monthlyAverageAttendance = dailySummaries.Any()
                ? dailySummaries.Average(ds => ds.AttendanceRate)
                : 0;

            var totalStudentsWithPaidLessons = dailySummaries.Any()
                ? dailySummaries.Max(ds => ds.StudentsWithPaidLessons)
                : 0;

            var totalPresentDays = dailySummaries.Sum(ds => ds.PresentStudents);
            var totalAbsentDays = dailySummaries.Sum(ds => ds.AbsentStudents);

            var result = new MonthlyAttendanceStatisticsDto
            {
                Month = month,
                Year = year,
                DailySummaries = dailySummaries,
                AbsentStudents = absentStudents,
                MonthlyAverageAttendance = Math.Round(monthlyAverageAttendance, 2),
                TotalStudentsWithPaidLessons = totalStudentsWithPaidLessons,
                TotalPresentDays = totalPresentDays,
                TotalAbsentDays = totalAbsentDays
            };

            return new Response<MonthlyAttendanceStatisticsDto>
            {
                Data = result,
                StatusCode = 200,
                Message = string.Format(Messages.AttendanceStatistics.MonthlySummarySuccess, month, year)
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting monthly attendance statistics for {Month}/{Year}", month, year);
            return new Response<MonthlyAttendanceStatisticsDto>(HttpStatusCode.InternalServerError,
                Messages.AttendanceStatistics.MonthlySummaryError);
        }
    }

    #endregion

    #region GetWeeklyAttendanceSummaryAsync

    public async Task<Response<List<DailyAttendanceSummaryDto>>> GetWeeklyAttendanceSummaryAsync(DateTime startDate,
        DateTime endDate, int? centerId = null)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var user = httpContextAccessor.HttpContext?.User;
            var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value)
                .ToList();
            bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");

            var effectiveCenterId = isSuperAdmin ? centerId : userCenterId;

            var dailySummaries = new List<DailyAttendanceSummaryDto>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dailySummary = await GetDailyAttendanceSummaryAsync(date, effectiveCenterId);
                dailySummaries.Add(dailySummary.Data);
            }

            return new Response<List<DailyAttendanceSummaryDto>>
            {
                Data = dailySummaries,
                StatusCode = 200,
                Message = string.Format(Messages.AttendanceStatistics.WeeklySummarySuccess,
                    startDate.ToString("dd.MM.yyyy"), endDate.ToString("dd.MM.yyyy"))
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting weekly attendance summary from {StartDate} to {EndDate}", startDate, endDate);
            return new Response<List<DailyAttendanceSummaryDto>>(HttpStatusCode.InternalServerError,
                Messages.AttendanceStatistics.WeeklySummaryError);
        }
    }

    #endregion

    #region GetGroupAttendanceForDateAsync

    public async Task<Response<List<StudentAttendanceStatisticsDto>>> GetGroupAttendanceForDateAsync(int groupId,
        DateTime date)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var user = httpContextAccessor.HttpContext?.User;
            var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value)
                .ToList();
            bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");

            var group = await db.Groups.Include(group => group.Mentor!)
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

            if (group == null)
                return new Response<List<StudentAttendanceStatisticsDto>>(HttpStatusCode.NotFound,
                    Messages.Group.NotFound);

            if (!isSuperAdmin && userCenterId != null && group.Mentor!.CenterId != userCenterId)
                return new Response<List<StudentAttendanceStatisticsDto>>(HttpStatusCode.Forbidden,
                    Messages.Common.AccessDenied);

            var students = await db.StudentGroups
                .Where(sg => sg.GroupId == groupId &&
                             sg.IsActive &&
                             !sg.IsDeleted &&
                             sg.Student!.PaymentStatus == PaymentStatus.Paid)
                .Select(sg => new StudentAttendanceStatisticsDto
                {
                    StudentId = sg.StudentId,
                    StudentName = sg.Student!.FullName,
                    GroupId = groupId,
                    GroupName = group.Name,
                    TotalLessons = 1,
                    PresentCount = db.JournalEntries
                        .Count(je => je.StudentId == sg.StudentId &&
                                     je.EntryDate.Date == date.Date &&
                                     je.AttendanceStatus == AttendanceStatus.Present &&
                                     !je.IsDeleted),
                    AbsentCount = db.JournalEntries
                        .Count(je => je.StudentId == sg.StudentId &&
                                     je.EntryDate.Date == date.Date &&
                                     je.AttendanceStatus == AttendanceStatus.Absent &&
                                     !je.IsDeleted),
                    LateCount = db.JournalEntries
                        .Count(je => je.StudentId == sg.StudentId &&
                                     je.EntryDate.Date == date.Date &&
                                     je.AttendanceStatus == AttendanceStatus.Late &&
                                     !je.IsDeleted),
                    StartDate = DateTimeOffset.MinValue,
                    EndDate = DateTimeOffset.MaxValue
                })
                .ToListAsync();

            foreach (var student in students)
            {
                student.AttendancePercentage = student.TotalLessons > 0
                    ? Math.Round((double)(student.PresentCount + student.LateCount) / student.TotalLessons * 100, 2)
                    : 0;
            }

            return new Response<List<StudentAttendanceStatisticsDto>>
            {
                Data = students,
                StatusCode = 200,
                Message = string.Format(Messages.AttendanceStatistics.GroupAttendanceSuccess, groupId,
                    date.ToString("dd.MM.yyyy"))
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting group attendance for date {Date}", date);
            return new Response<List<StudentAttendanceStatisticsDto>>(HttpStatusCode.InternalServerError,
                Messages.AttendanceStatistics.GroupAttendanceError);
        }
    }

    #endregion

    #region GetStudentsWithPaidLessonsButAbsentAsync

    public async Task<Response<List<AbsentStudentDto>>> GetStudentsWithPaidLessonsButAbsentAsync(DateTime date,
        int? centerId = null)
    {
        return await GetAbsentStudentsAsync(date, centerId);
    }

    #endregion

    #region GetStudentsWithPaidLessonsAndPresentAsync

    public async Task<Response<List<StudentAttendanceStatisticsDto>>> GetStudentsWithPaidLessonsAndPresentAsync(
        DateTime date, int? centerId = null)
    {
        try
        {
            if (date == default(DateTime))
                date = DateTime.Now.Date;

            var userCenterId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var user = httpContextAccessor.HttpContext?.User;
            var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value)
                .ToList();
            bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");

            var effectiveCenterId = isSuperAdmin ? centerId : userCenterId;

            var presentStudents = new List<StudentAttendanceStatisticsDto>();

            var groupsWithLessonsToday = await db.Groups
                .Where(g => !g.IsDeleted &&
                            g.Started &&
                            g.Status == ActiveStatus.Active &&
                            g.LessonDays != null &&
                            g.LessonStartTime != null &&
                            g.LessonEndTime != null &&
                            (effectiveCenterId == null || g.Mentor!.CenterId == effectiveCenterId))
                .ToListAsync();

            foreach (var group in groupsWithLessonsToday)
            {
                if (group.LessonDays == null || group.LessonStartTime == null || group.LessonEndTime == null)
                    continue;

                var dayOfWeek = (int)date.DayOfWeek;
                var lessonDays = group.LessonDays.Split(',').Select(int.Parse).ToList();

                if (!lessonDays.Contains(dayOfWeek))
                    continue;

                var lessonStartTime = group.LessonStartTime.Value;
                var currentTime = TimeOnly.FromDateTime(DateTime.Now);

                if (currentTime >= lessonStartTime)
                {
                    var groupStudents = await db.StudentGroups
                        .Where(sg => sg.GroupId == group.Id &&
                                     sg.IsActive &&
                                     !sg.IsDeleted &&
                                     sg.Student!.PaymentStatus == PaymentStatus.Paid)
                        .Include(studentGroup => studentGroup.Student)
                        .ToListAsync();

                    foreach (var studentGroup in groupStudents)
                    {
                        var journalEntry = await db.JournalEntries
                            .Where(je => je.StudentId == studentGroup.StudentId &&
                                         je.EntryDate.Date == date.Date &&
                                         !je.IsDeleted)
                            .FirstOrDefaultAsync();

                        if (journalEntry != null &&
                            (journalEntry.AttendanceStatus == AttendanceStatus.Present ||
                             journalEntry.AttendanceStatus == AttendanceStatus.Late))
                        {
                            presentStudents.Add(new StudentAttendanceStatisticsDto
                            {
                                StudentId = studentGroup.StudentId,
                                StudentName = studentGroup.Student!.FullName,
                                GroupId = group.Id,
                                GroupName = group.Name,
                                TotalLessons = 1,
                                PresentCount = journalEntry.AttendanceStatus == AttendanceStatus.Present ? 1 : 0,
                                AbsentCount = 0,
                                LateCount = journalEntry.AttendanceStatus == AttendanceStatus.Late ? 1 : 0,
                                AttendancePercentage = 100,
                                StartDate = DateTimeOffset.MinValue,
                                EndDate = DateTimeOffset.MaxValue
                            });
                        }
                    }
                }
            }

            return new Response<List<StudentAttendanceStatisticsDto>>
            {
                Data = presentStudents,
                StatusCode = 200,
                Message = string.Format(Messages.AttendanceStatistics.PresentStudentsSuccess, presentStudents.Count)
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting present students with paid lessons for date {Date}", date);
            return new Response<List<StudentAttendanceStatisticsDto>>(HttpStatusCode.InternalServerError,
                Messages.AttendanceStatistics.PresentStudentsError);
        }
    }

    #endregion
}