using Domain.DTOs.Schedule;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScheduleController(IScheduleService scheduleService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await scheduleService.CreateScheduleAsync(createDto);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        if (response.StatusCode == 409) 
        {
            return Conflict(response);
        }
        
        return BadRequest(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSchedule(int id)
    {
        var response = await scheduleService.GetScheduleByIdAsync(id);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return NotFound(response);
    }

    [HttpGet("classroom/{classroomId}")]
    public async Task<IActionResult> GetSchedulesByClassroom(
        int classroomId,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null)
    {
        var response = await scheduleService.GetSchedulesByClassroomAsync(classroomId, startDate, endDate);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpGet("group/{groupId}")]
    public async Task<IActionResult> GetSchedulesByGroup(int groupId)
    {
        var response = await scheduleService.GetSchedulesByGroupAsync(groupId);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSchedule([FromBody] UpdateScheduleDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await scheduleService.UpdateScheduleAsync(updateDto);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        if (response.StatusCode == 409) // Conflict
        {
            return Conflict(response);
        }
        
        return BadRequest(response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        var response = await scheduleService.DeleteScheduleAsync(id);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpPost("check-conflict")]
    public async Task<IActionResult> CheckScheduleConflict([FromBody] CreateScheduleDto scheduleDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await scheduleService.CheckScheduleConflictAsync(scheduleDto);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpGet("classroom/{classroomId}/available-slots")]
    public async Task<IActionResult> GetAvailableTimeSlots(
        int classroomId,
        [FromQuery] DayOfWeek dayOfWeek,
        [FromQuery] DateOnly date,
        [FromQuery] int durationMinutes = 90)
    {
        var duration = TimeSpan.FromMinutes(durationMinutes);
        var response = await scheduleService.GetAvailableTimeSlotsAsync(classroomId, dayOfWeek, date, duration);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpGet("classroom/{classroomId}/check-availability")]
    public async Task<IActionResult> IsTimeSlotAvailable(
        int classroomId,
        [FromQuery] DayOfWeek dayOfWeek,
        [FromQuery] TimeOnly startTime,
        [FromQuery] TimeOnly endTime,
        [FromQuery] DateOnly date)
    {
        var response = await scheduleService.IsTimeSlotAvailableAsync(classroomId, dayOfWeek, startTime, endTime, date);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpGet("center/{centerId}/weekly")]
    public async Task<IActionResult> GetWeeklySchedule(
        int centerId,
        [FromQuery] DateOnly weekStart)
    {
        var response = await scheduleService.GetWeeklyScheduleAsync(centerId, weekStart);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }
} 