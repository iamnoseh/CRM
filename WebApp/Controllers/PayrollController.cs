using Domain.DTOs.Payroll;
using Domain.Filters;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin,Manager")]
public class PayrollController(IPayrollService service) : ControllerBase
{
    #region WorkLog

    #region POST api/payroll/worklog

    [HttpPost("worklog")]
    public async Task<IActionResult> CreateWorkLog([FromBody] CreateWorkLogDto dto)
    {
        var result = await service.CreateWorkLogAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region GET api/payroll/worklog

    [HttpGet("worklog")]
    public async Task<IActionResult> GetWorkLogs([FromQuery] int? mentorId, [FromQuery] int? employeeUserId, [FromQuery] int month, [FromQuery] int year)
    {
        var result = await service.GetWorkLogsAsync(mentorId, employeeUserId, month, year);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region GET api/payroll/worklog/hours

    [HttpGet("worklog/hours")]
    public async Task<IActionResult> GetTotalHours([FromQuery] int? mentorId, [FromQuery] int? employeeUserId, [FromQuery] int month, [FromQuery] int year)
    {
        var result = await service.GetTotalHoursAsync(mentorId, employeeUserId, month, year);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region DELETE api/payroll/worklog/{id}

    [HttpDelete("worklog/{id}")]
    public async Task<IActionResult> DeleteWorkLog(int id)
    {
        var result = await service.DeleteWorkLogAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #endregion

    #region PayrollRecord

    #region POST api/payroll/calculate

    [HttpPost("calculate")]
    public async Task<IActionResult> CalculatePayroll([FromBody] CalculatePayrollDto dto)
    {
        var result = await service.CalculatePayrollAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region POST api/payroll/calculate-all

    [HttpPost("calculate-all")]
    public async Task<IActionResult> CalculateAllForMonth([FromQuery] int month, [FromQuery] int year)
    {
        var result = await service.CalculateAllForMonthAsync(month, year);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region POST api/payroll/bonus-fine

    [HttpPost("bonus-fine")]
    public async Task<IActionResult> AddBonusFine([FromBody] AddBonusFineDto dto)
    {
        var result = await service.AddBonusFineAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region POST api/payroll/{id}/approve

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await service.ApproveAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region POST api/payroll/{id}/pay

    [HttpPost("{id}/pay")]
    public async Task<IActionResult> MarkAsPaid(int id, [FromBody] MarkAsPaidDto dto)
    {
        var result = await service.MarkAsPaidAsync(id, dto);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region GET api/payroll/records

    [HttpGet("records")]
    public async Task<IActionResult> GetPayrollRecords([FromQuery] int month, [FromQuery] int year)
    {
        var result = await service.GetPayrollRecordsAsync(month, year);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region GET api/payroll/records/paginated

    [HttpGet("records/paginated")]
    public async Task<IActionResult> GetPayrollRecordsPaginated([FromQuery] PayrollFilter filter)
    {
        var result = await service.GetPayrollRecordsPaginatedAsync(filter);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region GET api/payroll/records/{id}

    [HttpGet("records/{id}")]
    public async Task<IActionResult> GetPayrollRecordById(int id)
    {
        var result = await service.GetPayrollRecordByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #endregion

    #region Advance

    #region POST api/payroll/advance

    [HttpPost("advance")]
    public async Task<IActionResult> CreateAdvance([FromBody] CreateAdvanceDto dto)
    {
        var result = await service.CreateAdvanceAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region GET api/payroll/advances

    [HttpGet("advances")]
    public async Task<IActionResult> GetAdvances([FromQuery] int? mentorId, [FromQuery] int? employeeUserId, [FromQuery] int? month, [FromQuery] int? year)
    {
        var result = await service.GetAdvancesAsync(mentorId, employeeUserId, month, year);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region GET api/payroll/advances/pending-amount

    [HttpGet("advances/pending-amount")]
    public async Task<IActionResult> GetPendingAdvancesAmount([FromQuery] int? mentorId, [FromQuery] int? employeeUserId, [FromQuery] int month, [FromQuery] int year)
    {
        var result = await service.GetPendingAdvancesAmountAsync(mentorId, employeeUserId, month, year);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region DELETE api/payroll/advance/{id}

    [HttpDelete("advance/{id}")]
    public async Task<IActionResult> CancelAdvance(int id)
    {
        var result = await service.CancelAdvanceAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #endregion

    #region Payment History & Status

    #region GET api/payroll/payment-history

    [HttpGet("payment-history")]
    public async Task<IActionResult> GetPaymentHistory([FromQuery] int? mentorId, [FromQuery] int? employeeUserId, [FromQuery] int? month, [FromQuery] int? year)
    {
        var result = await service.GetPaymentHistoryAsync(mentorId, employeeUserId, month, year);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #endregion

    #region Reports

    #region GET api/payroll/summary

    [HttpGet("summary")]
    public async Task<IActionResult> GetMonthlySummary([FromQuery] int month, [FromQuery] int year)
    {
        var result = await service.GetMonthlySummaryAsync(month, year);
        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #endregion
}
