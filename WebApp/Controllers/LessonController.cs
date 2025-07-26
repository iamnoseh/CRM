using Domain.DTOs.Lesson;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LessonController(ILessonService lessonService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateLesson([FromBody] CreateLessonDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await lessonService.CreateLessonAsync(createDto);
        
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLesson(int id)
    {
        var response = await lessonService.GetLessonByIdAsync(id);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return NotFound(response);
    }

    [HttpGet("group/{groupId}")]
    public async Task<IActionResult> GetLessonsByGroup(int groupId)
    {
        var response = await lessonService.GetLessonsByGroupAsync(groupId);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpGet("classroom/{classroomId}")]
    public async Task<IActionResult> GetLessonsByClassroom(
        int classroomId,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null)
    {
        var response = await lessonService.GetLessonsByClassroomAsync(classroomId, startDate, endDate);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpGet("schedule/{scheduleId}")]
    public async Task<IActionResult> GetLessonsBySchedule(int scheduleId)
    {
        var response = await lessonService.GetLessonsByScheduleAsync(scheduleId);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateLesson([FromBody] UpdateLessonDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await lessonService.UpdateLessonAsync(updateDto);
        
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
    public async Task<IActionResult> DeleteLesson(int id)
    {
        var response = await lessonService.DeleteLessonAsync(id);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpGet("group/{groupId}/weekly")]
    public async Task<IActionResult> GetWeeklyLessons(
        int groupId,
        [FromQuery] DateOnly weekStart)
    {
        var response = await lessonService.GetWeeklyLessonsAsync(groupId, weekStart);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpGet("classroom/{classroomId}/can-schedule")]
    public async Task<IActionResult> CanScheduleLesson(
        int classroomId,
        [FromQuery] DateTimeOffset startTime,
        [FromQuery] DateTimeOffset endTime)
    {
        var response = await lessonService.CanScheduleLessonAsync(classroomId, startTime, endTime);
        
        if (response.StatusCode == 200)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }
} 