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
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Student")]
    public async Task<ActionResult<PaginationResponse<List<AccountListItemDto>>>> List([FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var res = await service.GetAccountsAsync(search, pageNumber, pageSize);
        return StatusCode(res.StatusCode, res);
    }

    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<Response<Domain.DTOs.Finance.MyWalletDto>>> GetMyWallet([FromQuery] int limit = 10)
    {
        var res = await service.GetMyWalletAsync(limit);
        return StatusCode(res.StatusCode, res);
    }
    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Student")]
    public async Task<ActionResult<Response<GetStudentAccountDto>>> GetByStudent(int studentId)
    {
        var res = await service.GetByStudentIdAsync(studentId);
        return StatusCode(res.StatusCode, res);
    }

    [HttpGet("student/{studentId}/logs")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Student")]
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

    [HttpPost("group-charge-run")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<int>>> GroupMonthlyChargeRun([FromQuery] int groupId, [FromQuery] int? month = null, [FromQuery] int? year = null)
    {
        var now = DateTime.UtcNow;
        var m = month ?? now.Month;
        var y = year ?? now.Year;
        var res = await service.RunMonthlyChargeForGroupAsync(groupId, m, y);
        return StatusCode(res.StatusCode, res);
    }

    [HttpPost("manual-charge")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> ManualChargeForStudentGroup([FromQuery] int studentId, [FromQuery] int groupId, [FromQuery] int? month = null, [FromQuery] int? year = null)
    {
        var now = DateTime.UtcNow;
        var m = month ?? now.Month;
        var y = year ?? now.Year;
        var res = await service.ChargeForGroupAsync(studentId, groupId, m, y);
        return StatusCode(res.StatusCode, res);
    }
}


