// using Domain.DTOs.Attendance;
// using Domain.Entities;
// using Domain.Responses;
// using Infrastructure.Interfaces;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
//
// namespace WebApp.Controllers;
// [ApiController]
// [Route("api/[controller]")]
// public class AttendanceController (IAttendanceService service) : ControllerBase
// {
//     [HttpGet]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.Student},{Roles.SuperAdmin},{Roles.Manager}")]
//     public async Task<Response<List<GetAttendanceDto>>> GetAttendances() => 
//         await service.GetAttendances();
//         
//     [HttpGet("{id}")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.Student},{Roles.SuperAdmin},{Roles.Manager}")]
//     public async Task<Response<GetAttendanceDto>> GetAttendance(int id) => 
//         await service.GetAttendanceById(id);
//         
//     [HttpGet("student/{studentId}")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.Student},{Roles.SuperAdmin},{Roles.Manager}")]
//     public async Task<Response<List<GetAttendanceDto>>> GetAttendancesByStudent(int studentId) =>
//         await service.GetAttendancesByStudent(studentId);
//         
//     [HttpGet("group/{groupId}")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.Student},{Roles.SuperAdmin},{Roles.Manager}")]
//     public async Task<Response<List<GetAttendanceDto>>> GetAttendancesByGroup(int groupId) =>
//         await service.GetAttendancesByGroup(groupId);
//         
//     [HttpGet("lesson/{lessonId}")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.Student},{Roles.SuperAdmin},{Roles.Manager}")]
//     public async Task<Response<List<GetAttendanceDto>>> GetAttendancesByLesson(int lessonId) =>
//         await service.GetAttendancesByLesson(lessonId);
//
//     [HttpPost]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> CreateAttendance(AddAttendanceDto request) =>
//         await service.CreateAttendance(request);
//     
//     [HttpPut]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> UpdateAttendance(EditAttendanceDto request) =>
//         await service.EditAttendance(request);
//     
//     [HttpDelete("{id}")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> DeleteAttendance(int id) =>
//         await service.DeleteAttendanceById(id);
// }