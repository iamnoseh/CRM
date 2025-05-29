using Domain.DTOs.Statistics;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceStatisticsController(IAttendanceStatisticsService attendanceStatisticsService) : ControllerBase
{
    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "Admin,Teacher,Student,Manager")]
    public async Task<ActionResult<Response<StudentAttendanceAllStatisticsDto>>> GetStudentStatistics(
        int studentId, 
        [FromQuery] int? groupId = null,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        var response = await attendanceStatisticsService.GetStudentAttendanceStatisticsAsync(
            studentId, groupId, startDate, endDate);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("group/{groupId}")]
    [Authorize(Roles = "Admin,Teacher,Student,Manager")]
    public async Task<ActionResult<Response<GroupAttendanceAllStatisticsDto>>> GetGroupStatistics(
        int groupId,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        var response = await attendanceStatisticsService.GetGroupAttendanceStatisticsAsync(
            groupId, startDate, endDate);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("group/{groupId}/daily")]
    [Authorize(Roles = "Admin,Teacher,Student,Manager")]
    public async Task<ActionResult<Response<List<GroupAttendanceAllStatisticsDto>>>> GetDailyGroupStatistics(
        int groupId,
        [FromQuery] DateTimeOffset date)
    {
        var response = await attendanceStatisticsService.GetDailyGroupAttendanceStatisticsAsync(
            groupId, date);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("center/{centerId}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<Response<CenterAttendanceAllStatisticsDto>>> GetCenterStatistics(
        int centerId,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        var response = await attendanceStatisticsService.GetCenterAttendanceStatisticsAsync(
            centerId, startDate, endDate);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("center/{centerId}/daily")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<Response<List<CenterAttendanceAllStatisticsDto>>>> GetDailyCenterStatistics(
        int centerId,
        [FromQuery] DateTimeOffset date)
    {
        var response = await attendanceStatisticsService.GetDailyCenterAttendanceStatisticsAsync(
            centerId, date);
        return StatusCode(response.StatusCode, response);
    }
}
