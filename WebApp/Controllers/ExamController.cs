using System.Net;
using Domain.DTOs.Exam;
using Domain.DTOs.Grade;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExamController(IExamService examService, IGradeService gradeService) : ControllerBase
{
    #region Exam Endpoints
    
    [HttpGet]    public async Task<ActionResult<Response<List<GetExamDto>>>> GetAllExams()
    {
        var response = await examService.GetExams();
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("exam/{id}")]
    public async Task<ActionResult<Response<GetExamDto>>> GetExamById(int id)
    {
        var response = await examService.GetExamById(id);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<Response<List<GetExamDto>>>> GetExamsByGroup(int groupId)
    {
        var response = await examService.GetExamsByGroup(groupId);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> CreateExam([FromBody] CreateExamDto createExamDto)
    {
        var response = await examService.CreateExam(createExamDto);
        return StatusCode(response.StatusCode, response);
    }
      [HttpPut("exam/{id}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> UpdateExam(int id, [FromBody] UpdateExamDto updateExamDto)
    {
        var response = await examService.UpdateExam(id, updateExamDto);
        return StatusCode(response.StatusCode, response);
    }
      [HttpDelete("exam/{id}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> DeleteExam(int id)
    {
        var response = await examService.DeleteExam(id);
        return StatusCode(response.StatusCode, response);
    }
    
    #endregion
    
    #region ExamGrade Endpoints
    
    [HttpGet("grade/{id}")]
    public async Task<ActionResult<Response<GetGradeDto>>> GetExamGradeById(int id)
    {
        var response = await gradeService.GetGradeByIdAsync(id);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("{examId}/grades")]
    [Authorize(Roles = "Admin,Teacher,Manager")]
    public async Task<ActionResult<Response<List<GetGradeDto>>>> GetGradesByExam(int examId)
    {
        var response = await gradeService.GetGradesByExamAsync(examId);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("student/{studentId}/grades")]
    public async Task<ActionResult<Response<List<GetGradeDto>>>> GetStudentExamGrades(int studentId)
    {
        // Получаем все оценки студента и фильтруем только те, у которых есть ExamId
        var response = await gradeService.GetGradesByStudentAsync(studentId);
        
        if (response.StatusCode != 200)
            return StatusCode(response.StatusCode, response);
            
        var examGrades = new Response<List<GetGradeDto>>(
            response.Data.Where(g => g.ExamId.HasValue).ToList());
            
        return StatusCode(200, examGrades);
    }
    
    [HttpPost("grade")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> CreateExamGrade([FromBody] CreateGradeDto gradeDto)
    {
        var response = await gradeService.CreateExamGradeAsync(gradeDto);
        return StatusCode(response.StatusCode, response);
    }
      [HttpPut("grade/{id}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> UpdateExamGrade(int id, [FromBody] UpdateGradeDto updateGradeDto)
    {
        var grade = await gradeService.GetGradeByIdAsync(id);
        if (grade?.StatusCode != 200 || grade?.Data == null)
            return StatusCode(grade?.StatusCode ?? 500, new Response<string>("Оценка не найдена"));
            
        if (!grade.Data.ExamId.HasValue)
            return BadRequest(new Response<string>("Это не является оценкой за экзамен"));
        
        var response = await gradeService.EditGradeAsync(updateGradeDto);
        return StatusCode(response.StatusCode, response);
    }
      [HttpDelete("grade/{id}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> DeleteExamGrade(int id)
    {
        // Проверяем, что оценка существует и является оценкой за экзамен
        var grade = await gradeService.GetGradeByIdAsync(id);
        if (grade?.StatusCode != 200 || grade?.Data == null)
            return StatusCode(grade?.StatusCode ?? 500, new Response<string>("Оценка не найдена"));
            
        if (!grade.Data.ExamId.HasValue)
            return BadRequest(new Response<string>("Это не является оценкой за экзамен"));
            
        var response = await gradeService.DeleteGradeAsync(id);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("student/{studentId}/average")]
    public async Task<ActionResult<Response<double>>> GetStudentExamAverage(int studentId, [FromQuery] int? groupId = null)
    {
        var response = await gradeService.GetStudentExamAverageAsync(studentId, groupId);
        return StatusCode(response.StatusCode, response);
    }
    
    #endregion
}