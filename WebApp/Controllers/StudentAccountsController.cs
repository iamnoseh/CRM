using Domain.DTOs.Finance;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentAccountsController(IStudentAccountService service) : ControllerBase
{
    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<GetStudentAccountDto>>> GetByStudent(int studentId)
    {
        var res = await service.GetByStudentIdAsync(studentId);
        return StatusCode(res.StatusCode, res);
    }

    [HttpGet("student/{studentId}/logs")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<List<GetAccountLogDto>>>> GetLogs(int studentId, [FromQuery] int limit = 10)
    {
        var res = await service.GetLastLogsAsync(studentId, limit);
        return StatusCode(res.StatusCode, res);
    }

    [HttpPost("topup")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<GetStudentAccountDto>>> TopUp([FromBody] TopUpDto dto)
    {
        var res = await service.TopUpAsync(dto);
        return StatusCode(res.StatusCode, res);
    }

    [HttpPost("monthly-charge-run")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<int>>> MonthlyChargeRun([FromQuery] int month, [FromQuery] int year)
    {
        var res = await service.RunMonthlyChargeAsync(month, year);
        return StatusCode(res.StatusCode, res);
    }
}


