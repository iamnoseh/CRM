using Domain.DTOs.Grade;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GradeController(IGradeService gradeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Response<List<GetGradeDto>>>> GetAllGrades()
    {
        var response = await gradeService.GetAllGradesAsync();
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Response<GetGradeDto>>> GetGradeById(int id)
    {
        var response = await gradeService.GetGradeByIdAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<Response<List<GetGradeDto>>>> GetGradesByStudent(int studentId)
    {
        var response = await gradeService.GetGradesByStudentAsync(studentId);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<Response<List<GetGradeDto>>>> GetGradesByGroup(int groupId)
    {
        var response = await gradeService.GetGradesByGroupAsync(groupId);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpGet("lesson/{lessonId}")]
    public async Task<ActionResult<Response<List<GetGradeDto>>>> GetGradesByLesson(int lessonId)
    {
        var response = await gradeService.GetGradesByLessonAsync(lessonId);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpGet("student/{studentId}/average")]
    public async Task<ActionResult<Response<double>>> GetStudentAverageGrade(int studentId, [FromQuery] int? groupId = null)
    {
        var response = await gradeService.GetStudentAverageGradeAsync(studentId, groupId);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> CreateGrade([FromBody] CreateGradeDto createGradeDto)
    {
        var response = await gradeService.CreateGradeAsync(createGradeDto);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpPut]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> UpdateGrade([FromBody] UpdateGradeDto updateGradeDto)
    {
        var response = await gradeService.EditGradeAsync(updateGradeDto);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> DeleteGrade(int id)
    {
        var response = await gradeService.DeleteGradeAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }
}