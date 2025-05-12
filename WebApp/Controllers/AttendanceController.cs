using Domain.DTOs.Attendance;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController(IAttendanceService attendanceService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Teacher,Student,Manager")]
    public async Task<ActionResult<Response<List<GetAttendanceDto>>>> GetAttendances()
    {
        var response = await attendanceService.GetAttendances();
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Teacher,Student,Manager")]
    public async Task<ActionResult<Response<GetAttendanceDto>>> GetAttendanceById(int id)
    {
        var response = await attendanceService.GetAttendanceById(id);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "Admin,Teacher,Student,Manager")]
    public async Task<ActionResult<Response<List<GetAttendanceDto>>>> GetAttendancesByStudent(int studentId)
    {
        var response = await attendanceService.GetAttendancesByStudent(studentId);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpGet("group/{groupId}")]
    [Authorize(Roles = "Admin,Teacher,Student,Manager")]
    public async Task<ActionResult<Response<List<GetAttendanceDto>>>> GetAttendancesByGroup(int groupId)
    {
        var response = await attendanceService.GetAttendancesByGroup(groupId);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpGet("lesson/{lessonId}")]
    [Authorize(Roles = "Admin,Teacher,Student,Manager")]
    public async Task<ActionResult<Response<List<GetAttendanceDto>>>> GetAttendancesByLesson(int lessonId)
    {
        var response = await attendanceService.GetAttendancesByLesson(lessonId);
        return StatusCode((int)response.StatusCode, response);
    }
    
    
    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> CreateAttendance([FromBody] AddAttendanceDto addAttendanceDto)
    {
        var response = await attendanceService.CreateAttendance(addAttendanceDto);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpPut]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> UpdateAttendance([FromBody] EditAttendanceDto editAttendanceDto)
    {
        var response = await attendanceService.EditAttendance(editAttendanceDto);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> DeleteAttendance(int id)
    {
        var response = await attendanceService.DeleteAttendanceById(id);
        return StatusCode((int)response.StatusCode, response);
    }
}