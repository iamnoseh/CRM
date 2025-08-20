
using Domain.DTOs.Journal;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JournalController(IJournalService journalService) : ControllerBase
{
    [HttpPost("generate/{groupId}/{weekNumber}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor")] 
    public async Task<Response<string>> Generate(int groupId, int weekNumber) =>
        await journalService.GenerateWeeklyJournalAsync(groupId, weekNumber);

    [HttpGet("{groupId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Teacher,Student")] 
    public async Task<Response<GetJournalDto>> Get(int groupId, [FromQuery] int? weekNumber)
        => weekNumber.HasValue
            ? await journalService.GetJournalAsync(groupId, weekNumber.Value)
            : await journalService.GetLatestJournalAsync(groupId);

    [HttpGet("{groupId}/by-date")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Teacher,Student")] 
    public async Task<Response<GetJournalDto>> GetByDate(int groupId, [FromQuery] DateTime date ) =>
        await journalService.GetJournalByDateAsync(groupId, date);

    [HttpPatch("entry/{entryId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor")] 
    public async Task<Response<string>> UpdateEntry(int entryId, [FromBody] UpdateJournalEntryDto dto) =>
        await journalService.UpdateEntryAsync(entryId, dto);

    [HttpGet("{groupId}/week/{weekNumber}/totals")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Teacher,Student")] 
    public async Task<Response<List<StudentWeekTotalsDto>>> GetWeekTotals(int groupId, int weekNumber) =>
        await journalService.GetStudentWeekTotalsAsync(groupId, weekNumber);
    
    [HttpGet("{groupId}/weekly-totals")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Teacher,Student")]
    public async Task<Response<GroupWeeklyTotalsDto>> GetGroupWeeklyTotals(int groupId, [FromQuery] int? weekId = null) =>
        await journalService.GetGroupWeeklyTotalsAsync(groupId, weekId);

    [HttpGet("{groupId}/pass-stats")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Teacher")]
    public async Task<Response<GroupPassStatsDto>> GetGroupPassStats(int groupId, [FromQuery] decimal threshold = 80)
        => await journalService.GetGroupPassStatsAsync(groupId, threshold);
}


