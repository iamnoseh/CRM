using Domain.DTOs.Statistics;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FinanceController(IFinanceService financeService) : ControllerBase
{
    [HttpGet("centers/{centerId}/summary")]
    [Authorize(Roles = "Manager,SuperAdmin")]
    public async Task<ActionResult<Response<CenterFinancialSummaryDto>>> GetSummary(
        int centerId,
        [FromQuery] DateTimeOffset start,
        [FromQuery] DateTimeOffset end)
    {
        var response = await financeService.GetFinancialSummaryAsync(centerId, start, end);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("centers/{centerId}/daily")]
    [Authorize(Roles = "Manager,SuperAdmin")]
    public async Task<ActionResult<Response<DailyFinancialSummaryDto>>> GetDaily(
        int centerId,
        [FromQuery] DateTimeOffset date)
    {
        var response = await financeService.GetDailySummaryAsync(centerId, date);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("centers/{centerId}/monthly")]
    [Authorize(Roles = "Manager,SuperAdmin")]
    public async Task<ActionResult<Response<MonthlyFinancialSummaryDto>>> GetMonthly(
        int centerId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var response = await financeService.GetMonthlySummaryAsync(centerId, year, month);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("centers/{centerId}/yearly")]
    [Authorize(Roles = "Manager,SuperAdmin")]
    public async Task<ActionResult<Response<YearlyFinancialSummaryDto>>> GetYearly(
        int centerId,
        [FromQuery] int year)
    {
        var response = await financeService.GetYearlySummaryAsync(centerId, year);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("centers/{centerId}/payroll")]
    [Authorize(Roles = "Manager,SuperAdmin")]
    public async Task<ActionResult<Response<int>>> GeneratePayroll(
        int centerId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var response = await financeService.GenerateMentorPayrollAsync(centerId, year, month);
        return StatusCode(response.StatusCode, response);
    }
}