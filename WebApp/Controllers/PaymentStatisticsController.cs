using Domain.DTOs.Statistics;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentStatisticsController(IPaymentStatisticsService paymentStatisticsService) : ControllerBase
{
    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "Admin,Manager,Teacher")]
    [ProducesResponseType(typeof(Response<StudentPaymentStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Response<StudentPaymentStatisticsDto>>> GetStudentStatistics(
        int studentId,
        [FromQuery] int? groupId = null,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        var response = await paymentStatisticsService.GetStudentPaymentStatisticsAsync(
            studentId, groupId, startDate, endDate);
        return StatusCode(response.StatusCode, response);
    }    /// <summary>
    /// Маълумоти пардохтҳои як гурӯҳро нишон медиҳад
    /// </summary>
    /// <param name="groupId">ID-и гурӯҳ</param>
    /// <param name="startDate">Санаи оғоз (ихтиёрӣ)</param>
    /// <param name="endDate">Санаи анҷом (ихтиёрӣ)</param>
    /// <returns>Маълумоти муфассал дар бораи пардохтҳои гурӯҳ</returns>
    [HttpGet("group/{groupId}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(Response<GroupPaymentStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Response<GroupPaymentStatisticsDto>>> GetGroupStatistics(
        int groupId,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        var response = await paymentStatisticsService.GetGroupPaymentStatisticsAsync(
            groupId, startDate, endDate);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Маълумоти пардохтҳои рӯзонаи як гурӯҳро нишон медиҳад
    /// </summary>
    /// <param name="groupId">ID-и гурӯҳ</param>
    /// <param name="date">Сана</param>
    /// <returns>Маълумоти пардохтҳои рӯзона</returns>
    [HttpGet("group/{groupId}/daily")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(Response<List<GroupPaymentStatisticsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Response<List<GroupPaymentStatisticsDto>>>> GetDailyGroupStatistics(
        int groupId,
        [FromQuery] DateTimeOffset date)
    {
        var response = await paymentStatisticsService.GetDailyGroupPaymentStatisticsAsync(
            groupId, date);
        return StatusCode(response.StatusCode, response);
    }    /// <summary>
    /// Маълумоти пардохтҳои моҳонаи як гурӯҳро нишон медиҳад
    /// </summary>
    /// <param name="groupId">ID-и гурӯҳ</param>
    /// <param name="year">Сол</param>
    /// <param name="month">Моҳ (1-12)</param>
    /// <returns>Маълумоти пардохтҳои моҳона</returns>
    [HttpGet("group/{groupId}/monthly")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(Response<GroupPaymentStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Response<GroupPaymentStatisticsDto>>> GetMonthlyGroupStatistics(
        int groupId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var response = await paymentStatisticsService.GetMonthlyGroupPaymentStatisticsAsync(
            groupId, year, month);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Маълумоти пардохтҳои як марказро нишон медиҳад
    /// </summary>
    /// <param name="centerId">ID-и марказ</param>
    /// <param name="startDate">Санаи оғоз (ихтиёрӣ)</param>
    /// <param name="endDate">Санаи анҷом (ихтиёрӣ)</param>
    /// <returns>Маълумоти муфассал дар бораи пардохтҳои марказ</returns>
    [HttpGet("center/{centerId}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(Response<CenterPaymentStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Response<CenterPaymentStatisticsDto>>> GetCenterStatistics(
        int centerId,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        var response = await paymentStatisticsService.GetCenterPaymentStatisticsAsync(
            centerId, startDate, endDate);
        return StatusCode(response.StatusCode, response);
    }    /// <summary>
    /// Маълумоти пардохтҳои рӯзонаи як марказро нишон медиҳад
    /// </summary>
    /// <param name="centerId">ID-и марказ</param>
    /// <param name="date">Сана</param>
    /// <returns>Маълумоти пардохтҳои рӯзона</returns>
    [HttpGet("center/{centerId}/daily")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(Response<List<CenterPaymentStatisticsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Response<List<CenterPaymentStatisticsDto>>>> GetDailyCenterStatistics(
        int centerId,
        [FromQuery] DateTimeOffset date)
    {
        var response = await paymentStatisticsService.GetDailyCenterPaymentStatisticsAsync(
            centerId, date);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Маълумоти пардохтҳои моҳонаи як марказро нишон медиҳад
    /// </summary>
    /// <param name="centerId">ID-и марказ</param>
    /// <param name="year">Сол</param>
    /// <param name="month">Моҳ (1-12)</param>
    /// <returns>Маълумоти пардохтҳои моҳона</returns>
    [HttpGet("center/{centerId}/monthly")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(Response<CenterPaymentStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Response<CenterPaymentStatisticsDto>>> GetMonthlyCenterStatistics(
        int centerId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var response = await paymentStatisticsService.GetMonthlyCenterPaymentStatisticsAsync(
            centerId, year, month);
        return StatusCode(response.StatusCode, response);
    }
}
