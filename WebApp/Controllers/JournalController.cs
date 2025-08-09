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

    [HttpGet("{groupId}/{weekNumber}")]
    public async Task<Response<GetJournalDto>> Get(int groupId, int weekNumber) =>
        await journalService.GetJournalAsync(groupId, weekNumber);

    [HttpPut("entry/{entryId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor")] 
    public async Task<Response<string>> UpdateEntry(int entryId, [FromBody] UpdateJournalEntryDto dto) =>
        await journalService.UpdateEntryAsync(entryId, dto);
}


