using Domain.DTOs.Exam;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExamController(IExamService examService) : ControllerBase
{
    #region Exam Endpoints
    
    [HttpGet]
    [Authorize(Roles = "Admin,Teacher,Student,Manager")]
    public async Task<ActionResult<Response<List<GetExamDto>>>> GetAllExams()
    {
        var response = await examService.GetExams();
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Teacher,Student,Manager")]
    public async Task<ActionResult<Response<GetExamDto>>> GetExamById(int id)
    {
        var response = await examService.GetExamById(id);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("group/{groupId}")]
    [Authorize(Roles = "Admin,Teacher,Student,Manager")]
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
    
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> UpdateExam(int id, [FromBody] UpdateExamDto updateExamDto)
    {
        var response = await examService.UpdateExam(id, updateExamDto);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> DeleteExam(int id)
    {
        var response = await examService.DeleteExam(id);
        return StatusCode(response.StatusCode, response);
    }
    
    #endregion
    
    #region ExamGrade Endpoints
    
    [HttpGet("grade/{id}")]
    [Authorize(Roles = "Admin,Teacher,Student,Manager")]
    public async Task<ActionResult<Response<GetExamGradeDto>>> GetExamGradeById(int id)
    {
        var response = await examService.GetExamGradeById(id);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("{examId}/grades")]
    [Authorize(Roles = "Admin,Teacher,Manager")]
    public async Task<ActionResult<Response<List<GetExamGradeDto>>>> GetExamGradesByExam(int examId)
    {
        var response = await examService.GetExamGradesByExam(examId);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("student/{studentId}/grades")]
    [Authorize(Roles = "Admin,Teacher,Student,Manager")]
    public async Task<ActionResult<Response<List<GetExamGradeDto>>>> GetExamGradesByStudent(int studentId)
    {
        var response = await examService.GetExamGradesByStudent(studentId);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPost("grade")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> CreateExamGrade([FromBody] CreateExamGradeDto createExamGradeDto)
    {
        var response = await examService.CreateExamGrade(createExamGradeDto);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPut("grade")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> UpdateExamGrade(int id, [FromBody] UpdateExamGradeDto updateExamGradeDto)
    {
        var response = await examService.UpdateExamGrade(id, updateExamGradeDto);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpDelete("grade/{id}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> DeleteExamGrade(int id)
    {
        var response = await examService.DeleteExamGrade(id);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPost("grade/{examGradeId}/bonus")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> AddBonusPoint(int examGradeId, [FromBody] int bonusPoints)
    {
        var response = await examService.AddBonusPoint(examGradeId, bonusPoints);
        return StatusCode(response.StatusCode, response);
    }
    
    #endregion
}