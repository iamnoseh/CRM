
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
    public async Task<Response<GetJournalDto>> Get(int groupId, [FromQuery] int? weekNumber)
        => weekNumber.HasValue
            ? await journalService.GetJournalAsync(groupId, weekNumber.Value)
            : await journalService.GetLatestJournalAsync(groupId);

    [HttpGet("{groupId}/by-date")]
    public async Task<Response<GetJournalDto>> GetByDate(int groupId, [FromQuery] DateTime date ) =>
        await journalService.GetJournalByDateAsync(groupId, date);

    [HttpPut("entry/{entryId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor")] 
    public async Task<Response<string>> UpdateEntry(int entryId, [FromBody] UpdateJournalEntryDto dto) =>
        await journalService.UpdateEntryAsync(entryId, dto);
}


