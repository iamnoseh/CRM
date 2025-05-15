using Domain.DTOs.Lesson;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LessonController(ILessonService lessonService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Response<List<GetLessonDto>>>> GetAllLessons()
    {
        var response = await lessonService.GetLessons();
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Response<GetLessonDto>>> GetLessonById(int id)
    {
        var response = await lessonService.GetLessonById(id);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("paginated")]
    public async Task<ActionResult<PaginationResponse<List<GetLessonDto>>>> GetLessonsPaginated([FromQuery] BaseFilter filter)
    {
        var response = await lessonService.GetLessonsPaginated(filter);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<Response<List<GetLessonDto>>>> GetLessonsByGroup(int groupId)
    {
        var response = await lessonService.GetLessonsByGroup(groupId);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> CreateLesson([FromBody] CreateLessonDto createLessonDto)
    {
        var response = await lessonService.CreateLesson(createLessonDto);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPost("weekly")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> CreateWeeklyLessons(int groupId, int weekIndex, [FromBody] DateTimeOffset startDate)
    {
        var response = await lessonService.CreateWeeklyLessons(groupId, weekIndex, startDate);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPut]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> UpdateLesson([FromBody] UpdateLessonDto updateLessonDto)
    {
        var response = await lessonService.UpdateLesson(updateLessonDto);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> DeleteLesson(int id)
    {
        var response = await lessonService.DeleteLesson(id);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPost("{lessonId}/mark-student-present/{studentId}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> MarkStudentPresent(int lessonId, int studentId)
    {
        var response = await lessonService.MarkStudentPresent(lessonId, studentId);
        return StatusCode(response.StatusCode, response);
    }
}