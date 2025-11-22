
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
    public async Task<IActionResult> Generate(int groupId, int weekNumber)
    {
        var response = await journalService.GenerateWeeklyJournalAsync(groupId, weekNumber);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("generate-from-date/{groupId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> GenerateFromCustomDate(int groupId, [FromQuery] DateTime startDate)
    {
        var response = await journalService.GenerateWeeklyJournalFromCustomDateAsync(groupId, startDate);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{groupId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")] 
    public async Task<IActionResult> Get(int groupId, [FromQuery] int? weekNumber)
    {
        var response = weekNumber.HasValue
            ? await journalService.GetJournalAsync(groupId, weekNumber.Value)
            : await journalService.GetLatestJournalAsync(groupId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{groupId}/by-date")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")] 
    public async Task<IActionResult> GetByDate(int groupId, [FromQuery] DateTime date)
    {
        var response = await journalService.GetJournalByDateAsync(groupId, date);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPatch("entry/{entryId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor")] 
    public async Task<IActionResult> UpdateEntry(int entryId, [FromBody] UpdateJournalEntryDto dto)
    {
        var response = await journalService.UpdateEntryAsync(entryId, dto);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{groupId}/week/{weekNumber}/totals")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")] 
    public async Task<IActionResult> GetWeekTotals(int groupId, int weekNumber)
    {
        var response = await journalService.GetStudentWeekTotalsAsync(groupId, weekNumber);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("{groupId}/weekly-totals")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
    public async Task<IActionResult> GetGroupWeeklyTotals(int groupId, [FromQuery] int? weekId = null)
    {
        var response = await journalService.GetGroupWeeklyTotalsAsync(groupId, weekId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{groupId}/pass-stats")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor")]
    public async Task<IActionResult> GetGroupPassStats(int groupId, [FromQuery] decimal threshold = 80)
    {
        var response = await journalService.GetGroupPassStatsAsync(groupId, threshold);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{groupId}/week-numbers")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
    public async Task<IActionResult> GetGroupWeekNumbers(int groupId)
    {
        var response = await journalService.GetGroupWeekNumbersAsync(groupId);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpDelete("{groupId}/week/{weekNumber}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> DeleteJournal(int groupId, int weekNumber)
    {
        var response = await journalService.DeleteJournalAsync(groupId, weekNumber);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpDelete("{groupId}/all")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> DeleteAllJournals(int groupId)
    {
        var response = await journalService.DeleteAllJournalsAsync(groupId);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("student/{studentId}/comments")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
    public async Task<IActionResult> GetStudentComments(int studentId)
    {
        var response = await journalService.GetStudentCommentsAsync(studentId);
        return StatusCode(response.StatusCode, response);
    }
}


