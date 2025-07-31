using Domain.DTOs.Classroom;
using Domain.DTOs.Schedule;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClassroomController(IClassroomService classroomService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> CreateClassroom([FromBody] CreateClassroomDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await classroomService.CreateClassroomAsync(createDto);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> GetAllClassrooms([FromQuery]ClassroomFilter filter)
    {
        var res = await classroomService.GetAllClassrooms(filter);
        if (res.StatusCode == 200)
        {
            return Ok(res);
        }

        return NotFound(res);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClassroom(int id)
    {
        var response = await classroomService.GetClassroomByIdAsync(id);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return NotFound(response);
    }

    [HttpGet("center/{centerId}")]
    public async Task<IActionResult> GetClassroomsByCenter(int centerId)
    {
        var response = await classroomService.GetClassroomsByCenterAsync(centerId);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpPut]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> UpdateClassroom([FromBody] UpdateClassroomDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await classroomService.UpdateClassroomAsync(updateDto);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> DeleteClassroom(int id)
    {
        var response = await classroomService.DeleteClassroomAsync(id);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpGet("{classroomId}/schedule")]
    public async Task<IActionResult> GetClassroomSchedule(
        int classroomId, 
        [FromQuery] DateOnly? startDate = null, 
        [FromQuery] DateOnly? endDate = null)
    {
        var response = await classroomService.GetClassroomScheduleAsync(classroomId, startDate, endDate);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpPost("schedule/check-conflict")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> CheckScheduleConflict([FromBody] CreateScheduleDto scheduleDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await classroomService.CheckScheduleConflictAsync(scheduleDto);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpPost("schedule")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await classroomService.CreateScheduleAsync(createDto);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpGet("{classroomId}/available-slots")]
    public async Task<IActionResult> GetAvailableTimeSlots(
        int classroomId,
        [FromQuery] DayOfWeek dayOfWeek,
        [FromQuery] DateOnly date,
        [FromQuery] int durationMinutes = 90)
    {
        var duration = TimeSpan.FromMinutes(durationMinutes);
        var response = await classroomService.GetAvailableTimeSlotsAsync(classroomId, dayOfWeek, date, duration);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpGet("center/{centerId}/available")]
    public async Task<IActionResult> GetAvailableClassrooms(
        int centerId,
        [FromQuery] DayOfWeek dayOfWeek,
        [FromQuery] TimeOnly startTime,
        [FromQuery] TimeOnly endTime,
        [FromQuery] DateOnly date)
    {
        var response = await classroomService.GetAvailableClassroomsAsync(centerId, dayOfWeek, startTime, endTime, date);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpGet("simple")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<PaginationResponse<List<GetSimpleClassroomDto>>> GetSimpleClassrooms([FromQuery] BaseFilter filter) =>
        await classroomService.GetSimpleClassrooms(filter);
} 