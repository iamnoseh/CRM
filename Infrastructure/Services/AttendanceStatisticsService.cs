using Domain.DTOs.Statistics;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Infrastructure.Services;

public class AttendanceStatisticsService(DataContext db, IHttpContextAccessor httpContextAccessor) : IAttendanceStatisticsService
{
    private readonly DataContext _db = db;
    private readonly IHttpContextAccessor _http = httpContextAccessor;

    public async Task<Response<DailyAttendanceSummaryDto>> GetDailyAttendanceSummaryAsync(DateTime date, int? centerId = null)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_http);
            var user = _http.HttpContext?.User;
            var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");
            
            var effectiveCenterId = isSuperAdmin ? centerId : userCenterId;

            // Донишҷӯёне ки вақти дарсиашон шудааст (пардохт кардаанд)
            var studentsWithPaidLessons = await _db.Students
                .Where(s => !s.IsDeleted && 
                           s.ActiveStatus == ActiveStatus.Active &&
                           s.PaymentStatus == PaymentStatus.Paid &&
                           (effectiveCenterId == null || s.CenterId == effectiveCenterId))
                .CountAsync();

            // Донишҷӯёне ки дар рӯзи мушаххас ҳозиранд
            var presentStudentsQuery = _db.JournalEntries
                .Where(je => je.EntryDate.Date == date.Date &&
                           je.AttendanceStatus == AttendanceStatus.Present &&
                           !je.IsDeleted);
            
            if (effectiveCenterId.HasValue)
            {
                presentStudentsQuery = presentStudentsQuery.Where(je => je.Student.CenterId == effectiveCenterId.Value);
            }
            
            var presentStudents = await presentStudentsQuery
                .Select(je => je.StudentId)
                .Distinct()
                .CountAsync();

            // Донишҷӯёне ки дар рӯзи мушаххас ғоибанд
            var absentStudentsQuery = _db.JournalEntries
                .Where(je => je.EntryDate.Date == date.Date &&
                           je.AttendanceStatus == AttendanceStatus.Absent &&
                           !je.IsDeleted);
            
            if (effectiveCenterId.HasValue)
            {
                absentStudentsQuery = absentStudentsQuery.Where(je => je.Student.CenterId == effectiveCenterId.Value);
            }
            
            var absentStudents = await absentStudentsQuery
                .Select(je => je.StudentId)
                .Distinct()
                .CountAsync();

            // Донишҷӯёне ки дар рӯзи мушаххас дер омадаанд
            var lateStudentsQuery = _db.JournalEntries
                .Where(je => je.EntryDate.Date == date.Date &&
                           je.AttendanceStatus == AttendanceStatus.Late &&
                           !je.IsDeleted);
            
            if (effectiveCenterId.HasValue)
            {
                lateStudentsQuery = lateStudentsQuery.Where(je => je.Student.CenterId == effectiveCenterId.Value);
            }
            
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

            return new Response<DailyAttendanceSummaryDto>(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting daily attendance summary for date {Date}. Error: {Error}", date, ex.Message);
            return new Response<DailyAttendanceSummaryDto>(System.Net.HttpStatusCode.InternalServerError, $"Хатогии дохилӣ: {ex.Message}");
        }
    }

    public async Task<Response<List<AbsentStudentDto>>> GetAbsentStudentsAsync(DateTime date, int? centerId = null)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_http);
            var user = _http.HttpContext?.User;
            var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");
            
            var effectiveCenterId = isSuperAdmin ? centerId : userCenterId;

            var absentStudents = new List<AbsentStudentDto>();

            // Ҳисоб кардани донишҷӯёни ғоиб бо назардошти вақти дарс
            var groupsWithLessonsToday = await _db.Groups
                .Where(g => !g.IsDeleted && 
                           g.Started && 
                           g.Status == ActiveStatus.Active &&
                           g.LessonDays != null &&
                           g.LessonStartTime != null &&
                           g.LessonEndTime != null &&
                           (effectiveCenterId == null || g.Mentor.CenterId == effectiveCenterId))
                .ToListAsync();

            foreach (var group in groupsWithLessonsToday)
            {
                if (group.LessonDays == null || group.LessonStartTime == null || group.LessonEndTime == null)
                    continue;

                // Санҷидани ки рӯзи ҷорӣ дарс дорад
                var dayOfWeek = (int)date.DayOfWeek;
                var lessonDays = group.LessonDays.Split(',').Select(int.Parse).ToList();
                
                if (!lessonDays.Contains(dayOfWeek))
                    continue;

                var lessonStartTime = group.LessonStartTime.Value;
                var currentTime = TimeOnly.FromDateTime(DateTime.Now);

                // Агар вақти ҷорӣ аз вақти оғози дарс калонтар бошад, дарс сар шудааст
                if (currentTime >= lessonStartTime)
                {
                    var groupStudents = await _db.StudentGroups
                        .Where(sg => sg.GroupId == group.Id && 
                                   sg.IsActive && 
                                   !sg.IsDeleted &&
                                   sg.Student.PaymentStatus == PaymentStatus.Paid)
                        .ToListAsync();

                    foreach (var studentGroup in groupStudents)
                    {
                        var journalEntry = await _db.JournalEntries
                            .Where(je => je.StudentId == studentGroup.StudentId &&
                                       je.EntryDate.Date == date.Date &&
                                       !je.IsDeleted)
                            .FirstOrDefaultAsync();

                        // Агар дар журнал қайд набошад ё ғоиб қайд шуда бошад
                        if (journalEntry == null || journalEntry.AttendanceStatus == AttendanceStatus.Absent)
                        {
                            var lastAttendanceDate = await _db.JournalEntries
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
                                    var wasPresent = await _db.JournalEntries
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
                                FullName = studentGroup.Student.FullName,
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

            return new Response<List<AbsentStudentDto>>(absentStudents);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting absent students for date {Date}", date);
            return new Response<List<AbsentStudentDto>>(System.Net.HttpStatusCode.InternalServerError, "Хатогии дохилӣ");
        }
    }

    public async Task<Response<MonthlyAttendanceStatisticsDto>> GetMonthlyAttendanceStatisticsAsync(int month, int year, int? centerId = null)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_http);
            var user = _http.HttpContext?.User;
            var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");
            
            var effectiveCenterId = isSuperAdmin ? centerId : userCenterId;

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var dailySummaries = new List<DailyAttendanceSummaryDto>();
            var absentStudents = new List<AbsentStudentDto>();

            // Ҳисоб кардани омори ҳар рӯз
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dailySummary = await GetDailyAttendanceSummaryAsync(date, effectiveCenterId);
                if (dailySummary.Data != null)
                {
                    dailySummaries.Add(dailySummary.Data);
                }
            }

            // Донишҷӯёни ғоиб дар тӯли моҳ
            var monthlyAbsentStudents = await GetAbsentStudentsAsync(endDate, effectiveCenterId);
            if (monthlyAbsentStudents.Data != null)
            {
                absentStudents = monthlyAbsentStudents.Data;
            }

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

            return new Response<MonthlyAttendanceStatisticsDto>(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting monthly attendance statistics for {Month}/{Year}", month, year);
            return new Response<MonthlyAttendanceStatisticsDto>(System.Net.HttpStatusCode.InternalServerError, "Хатогии дохилӣ");
        }
    }

    public async Task<Response<List<DailyAttendanceSummaryDto>>> GetWeeklyAttendanceSummaryAsync(DateTime startDate, DateTime endDate, int? centerId = null)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_http);
            var user = _http.HttpContext?.User;
            var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");
            
            var effectiveCenterId = isSuperAdmin ? centerId : userCenterId;

            var dailySummaries = new List<DailyAttendanceSummaryDto>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dailySummary = await GetDailyAttendanceSummaryAsync(date, effectiveCenterId);
                if (dailySummary.Data != null)
                {
                    dailySummaries.Add(dailySummary.Data);
                }
            }

            return new Response<List<DailyAttendanceSummaryDto>>(dailySummaries);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting weekly attendance summary from {StartDate} to {EndDate}", startDate, endDate);
            return new Response<List<DailyAttendanceSummaryDto>>(System.Net.HttpStatusCode.InternalServerError, "Хатогии дохилӣ");
        }
    }

    public async Task<Response<List<StudentAttendanceStatisticsDto>>> GetGroupAttendanceForDateAsync(int groupId, DateTime date)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_http);
            var user = _http.HttpContext?.User;
            var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");
            
            // Санҷидани дастрасӣ ба гурӯҳ
            var group = await _db.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            
            if (group == null)
                return new Response<List<StudentAttendanceStatisticsDto>>(System.Net.HttpStatusCode.NotFound, "Гурӯҳ ёфт нашуд");

            // Агар SuperAdmin набошад, санҷид ки гурӯҳ аз маркази корбар аст
            if (!isSuperAdmin && userCenterId != null && group.Mentor.CenterId != userCenterId)
                return new Response<List<StudentAttendanceStatisticsDto>>(System.Net.HttpStatusCode.Forbidden, "Дастрасӣ манъ аст");

            var students = await _db.StudentGroups
                .Where(sg => sg.GroupId == groupId && 
                           sg.IsActive && 
                           !sg.IsDeleted &&
                           sg.Student.PaymentStatus == PaymentStatus.Paid)
                .Select(sg => new StudentAttendanceStatisticsDto
                {
                    StudentId = sg.StudentId,
                    StudentName = sg.Student.FullName,
                    GroupId = groupId,
                    GroupName = group.Name,
                    TotalLessons = 1, 
                    PresentCount = _db.JournalEntries
                        .Count(je => je.StudentId == sg.StudentId &&
                                   je.EntryDate.Date == date.Date &&
                                   je.AttendanceStatus == AttendanceStatus.Present &&
                                   !je.IsDeleted),
                    AbsentCount = _db.JournalEntries
                        .Count(je => je.StudentId == sg.StudentId &&
                                   je.EntryDate.Date == date.Date &&
                                   je.AttendanceStatus == AttendanceStatus.Absent &&
                                   !je.IsDeleted),
                    LateCount = _db.JournalEntries
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

            return new Response<List<StudentAttendanceStatisticsDto>>(students);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting group attendance for date {Date}", date);
            return new Response<List<StudentAttendanceStatisticsDto>>(System.Net.HttpStatusCode.InternalServerError, "Хатогии дохилӣ");
        }
    }

    public async Task<Response<List<AbsentStudentDto>>> GetStudentsWithPaidLessonsButAbsentAsync(DateTime date, int? centerId = null)
    {
        return await GetAbsentStudentsAsync(date, centerId);
    }

    public async Task<Response<List<StudentAttendanceStatisticsDto>>> GetStudentsWithPaidLessonsAndPresentAsync(DateTime date, int? centerId = null)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_http);
            var user = _http.HttpContext?.User;
            var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");
            
            var effectiveCenterId = isSuperAdmin ? centerId : userCenterId;

            var presentStudents = new List<StudentAttendanceStatisticsDto>();

            // Ҳисоб кардани донишҷӯёни ҳозир бо назардошти вақти дарс
            var groupsWithLessonsToday = await _db.Groups
                .Where(g => !g.IsDeleted && 
                           g.Started && 
                           g.Status == ActiveStatus.Active &&
                           g.LessonDays != null &&
                           g.LessonStartTime != null &&
                           g.LessonEndTime != null &&
                           (effectiveCenterId == null || g.Mentor.CenterId == effectiveCenterId))
                .ToListAsync();

            foreach (var group in groupsWithLessonsToday)
            {
                if (group.LessonDays == null || group.LessonStartTime == null || group.LessonEndTime == null)
                    continue;

                // Санҷидани ки рӯзи ҷорӣ дарс дорад
                var dayOfWeek = (int)date.DayOfWeek;
                var lessonDays = group.LessonDays.Split(',').Select(int.Parse).ToList();
                
                if (!lessonDays.Contains(dayOfWeek))
                    continue;

                var lessonStartTime = group.LessonStartTime.Value;
                var currentTime = TimeOnly.FromDateTime(DateTime.Now);

                // Агар вақти ҷорӣ аз вақти оғози дарс калонтар бошад, дарс сар шудааст
                if (currentTime >= lessonStartTime)
                {
                    var groupStudents = await _db.StudentGroups
                        .Where(sg => sg.GroupId == group.Id && 
                                   sg.IsActive && 
                                   !sg.IsDeleted &&
                                   sg.Student.PaymentStatus == PaymentStatus.Paid)
                        .ToListAsync();

                    foreach (var studentGroup in groupStudents)
                    {
                        var journalEntry = await _db.JournalEntries
                            .Where(je => je.StudentId == studentGroup.StudentId &&
                                       je.EntryDate.Date == date.Date &&
                                       !je.IsDeleted)
                            .FirstOrDefaultAsync();

                        // Агар дар журнал ҳозир ё дер қайд шуда бошад
                        if (journalEntry != null && 
                            (journalEntry.AttendanceStatus == AttendanceStatus.Present || 
                             journalEntry.AttendanceStatus == AttendanceStatus.Late))
                        {
                            presentStudents.Add(new StudentAttendanceStatisticsDto
                            {
                                StudentId = studentGroup.StudentId,
                                StudentName = studentGroup.Student.FullName,
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

            return new Response<List<StudentAttendanceStatisticsDto>>(presentStudents);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting present students with paid lessons for date {Date}", date);
            return new Response<List<StudentAttendanceStatisticsDto>>(System.Net.HttpStatusCode.InternalServerError, "Хатогии дохилӣ");
        }
    }
}
