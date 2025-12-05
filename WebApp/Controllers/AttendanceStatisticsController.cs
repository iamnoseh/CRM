using Domain.DTOs.Statistics;
using Domain.Entities;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceStatisticsController(IAttendanceStatisticsService attendanceStatisticsService) : ControllerBase
{
    private readonly IAttendanceStatisticsService _attendanceStatisticsService = attendanceStatisticsService;

    [HttpGet("daily-summary")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager}")]
    public async Task<ActionResult<Response<DailyAttendanceSummaryDto>>> GetDailyAttendanceSummary(
        [FromQuery] DateTime? date = null,
        [FromQuery] int? centerId = null)
    {
        try
        {
            var result = await _attendanceStatisticsService.GetDailyAttendanceSummaryAsync(date ?? default(DateTime), centerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in GetDailyAttendanceSummary");
            return StatusCode(500, new Response<DailyAttendanceSummaryDto>
            {
                StatusCode = 500,
                Message = "Хатогии дохилӣ дар сервер",
                Data = null!
            });
        }
    }

    [HttpGet("absent-students")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager}")]
    public async Task<ActionResult<Response<List<AbsentStudentDto>>>> GetAbsentStudents(
        [FromQuery] DateTime? date = null,
        [FromQuery] int? centerId = null)
    {
        try
        {
            var result = await _attendanceStatisticsService.GetAbsentStudentsAsync(date ?? default(DateTime), centerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in GetAbsentStudents");
            return StatusCode(500, new Response<List<AbsentStudentDto>>
            {
                StatusCode = 500,
                Message = "Хатогии дохилӣ дар сервер",
                Data = null!
            });
        }
    }

    [HttpGet("monthly-statistics")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager}")]
    public async Task<ActionResult<Response<MonthlyAttendanceStatisticsDto>>> GetMonthlyAttendanceStatistics(
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] int? centerId = null)
    {
        try
        {
            if (month < 1 || month > 12)
                return BadRequest(new Response<MonthlyAttendanceStatisticsDto>(System.Net.HttpStatusCode.BadRequest, "Моҳ бояд аз 1 то 12 бошад"));

            var result = await _attendanceStatisticsService.GetMonthlyAttendanceStatisticsAsync(month, year, centerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in GetMonthlyAttendanceStatistics");
            return StatusCode(500, new Response<MonthlyAttendanceStatisticsDto>
            {
                StatusCode = 500,
                Message = "Хатогии дохилӣ дар сервер",
                Data = null!
            });
        }
    }

    [HttpGet("weekly-summary")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager}")]
    public async Task<ActionResult<Response<List<DailyAttendanceSummaryDto>>>> GetWeeklyAttendanceSummary(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? centerId = null)
    {
        try
        {
            if (startDate > endDate)
                return BadRequest(new Response<List<DailyAttendanceSummaryDto>>(System.Net.HttpStatusCode.BadRequest, "Санаи оғоз наметавонад аз санаи анҷом калонтар бошад"));

            var result = await _attendanceStatisticsService.GetWeeklyAttendanceSummaryAsync(startDate, endDate, centerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in GetWeeklyAttendanceSummary");
            return StatusCode(500, new Response<List<DailyAttendanceSummaryDto>>
            {
                StatusCode = 500,
                Message = "Хатогии дохилӣ дар сервер",
                Data = null!
            });
        }
    }

    [HttpGet("group-attendance")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager}")]
    public async Task<ActionResult<Response<List<StudentAttendanceStatisticsDto>>>> GetGroupAttendanceForDate(
        [FromQuery] int groupId,
        [FromQuery] DateTime date)
    {
        try
        {
            var result = await _attendanceStatisticsService.GetGroupAttendanceForDateAsync(groupId, date);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in GetGroupAttendanceForDate");
            return StatusCode(500, new Response<List<StudentAttendanceStatisticsDto>>
            {
                StatusCode = 500,
                Message = "Хатогии дохилӣ дар сервер",
                Data = null!
            });
        }
    }

    [HttpGet("paid-but-absent")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager}")]
    public async Task<ActionResult<Response<List<AbsentStudentDto>>>> GetStudentsWithPaidLessonsButAbsent(
        [FromQuery] DateTime? date = null,
        [FromQuery] int? centerId = null)
    {
        try
        {
            var result = await _attendanceStatisticsService.GetStudentsWithPaidLessonsButAbsentAsync(date ?? default(DateTime), centerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in GetStudentsWithPaidLessonsButAbsent");
            return StatusCode(500, new Response<List<AbsentStudentDto>>
            {
                StatusCode = 500,
                Message = "Хатогии дохилӣ дар сервер",
                Data = null!
            });
        }
    }

    [HttpGet("paid-and-present")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager}")]
    public async Task<ActionResult<Response<List<StudentAttendanceStatisticsDto>>>> GetStudentsWithPaidLessonsAndPresent(
        [FromQuery] DateTime? date = null,
        [FromQuery] int? centerId = null)
    {
        try
        {
            var result = await _attendanceStatisticsService.GetStudentsWithPaidLessonsAndPresentAsync(date ?? default(DateTime), centerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in GetStudentsWithPaidLessonsAndPresent");
            return StatusCode(500, new Response<List<StudentAttendanceStatisticsDto>>
            {
                StatusCode = 500,
                Message = "Хатогии дохилӣ дар сервер",
                Data = null!
            });
        }
    }

    [HttpGet("dashboard-summary")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager}")]
    public async Task<ActionResult<Response<object>>> GetDashboardSummary(
        [FromQuery] DateTime date,
        [FromQuery] int? centerId = null)
    {
        try
        {
            var dailySummary = await _attendanceStatisticsService.GetDailyAttendanceSummaryAsync(date, centerId);
            var absentStudents = await _attendanceStatisticsService.GetAbsentStudentsAsync(date, centerId);
            var presentStudents = await _attendanceStatisticsService.GetStudentsWithPaidLessonsAndPresentAsync(date, centerId);

            var dashboardData = new
            {
                Date = date,
                DailySummary = dailySummary.Data,
                AbsentStudents = absentStudents.Data,
                PresentStudents = presentStudents.Data,
                TotalAbsentCount = absentStudents.Data.Count,
                TotalPresentCount = presentStudents.Data.Count
            };

            return Ok(new Response<object>(dashboardData));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in GetDashboardSummary");
            return StatusCode(500, new Response<object>(System.Net.HttpStatusCode.InternalServerError, "Хатогии дохилӣ"));
        }
    }
}
