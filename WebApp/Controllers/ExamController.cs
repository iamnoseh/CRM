// using Domain.DTOs.Exam;
// using Domain.Entities;
// using Domain.Responses;
// using Infrastructure.Interfaces;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
//
// namespace WebApp.Controllers;
//
// [ApiController]
// [Route("api/[controller]")]
// public class ExamController(IExamService service) : ControllerBase
// {
//     [HttpGet]
//     public async Task<Response<List<GetExamDto>>> GetAllExams() => 
//         await service.GetExams();
//
//     [HttpGet("{id}")]
//     public async Task<Response<GetExamDto>> GetExamById(int id) => 
//         await service.GetExamById(id);
//     
//     [HttpGet("student/{studentId}")]
//     public async Task<Response<List<GetExamDto>>> GetExamsByStudent(int studentId) =>
//         await service.GetExamsByStudent(studentId);
//         
//     [HttpGet("group/{groupId}")]
//     public async Task<Response<List<GetExamDto>>> GetExamsByGroup(int groupId) =>
//         await service.GetExamsByGroup(groupId);
//         
//     [HttpGet("group/{groupId}/week/{weekIndex}")]
//     public async Task<Response<List<GetExamDto>>> GetExamsByGroupAndWeek(int groupId, int weekIndex) =>
//         await service.GetExamsByGroupAndWeek(groupId, weekIndex);
//     
//     [HttpGet("student/{studentId}/group/{groupId}/week/{weekIndex}")]
//     public async Task<Response<List<GetExamDto>>> GetStudentExamsByWeek(int studentId, int groupId, int weekIndex) =>
//         await service.GetStudentExamsByWeek(studentId, groupId, weekIndex);
//     
//     [HttpPost]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> CreateExam(CreateExamDto createExam) =>
//         await service.CreateExam(createExam);
//     
//     [HttpDelete("{id}")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> DeleteExam(int id) => 
//         await service.DeleteExam(id);
//     
//     [HttpPut("{id}")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> UpdateExam(int id, UpdateExamDto examDto) => 
//         await service.UpdateExam(id, examDto);
// } 