// using Domain.DTOs.Grade;
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
// public class GradeController(IGradeService service) : ControllerBase
// {
//     [HttpGet]
//     public async Task<Response<List<GetGradeDto>>> GetAllGrades() => 
//         await service.GetAllGradesAsync();
//
//     [HttpGet("{id}")]
//     public async Task<Response<GetGradeDto>> GetGradeById(int id) => 
//         await service.GetGradeByIdAsync(id);
//         
//     [HttpGet("student/{studentId}")]
//     public async Task<Response<List<GetGradeDto>>> GetGradesByStudent(int studentId) =>
//         await service.GetGradesByStudentAsync(studentId);
//         
//     [HttpGet("group/{groupId}")]
//     public async Task<Response<List<GetGradeDto>>> GetGradesByGroup(int groupId) =>
//         await service.GetGradesByGroupAsync(groupId);
//         
//     [HttpGet("lesson/{lessonId}")]
//     public async Task<Response<List<GetGradeDto>>> GetGradesByLesson(int lessonId) =>
//         await service.GetGradesByLessonAsync(lessonId);
//         
//     [HttpGet("group/{groupId}/week/{weekIndex}")]
//     public async Task<Response<List<GetGradeDto>>> GetGradesByWeek(int groupId, int weekIndex) =>
//         await service.GetGradesByWeekAsync(groupId, weekIndex);
//         
//     [HttpGet("student/{studentId}/group/{groupId}/week/{weekIndex}")]
//     public async Task<Response<List<GetGradeDto>>> GetStudentGradesByWeek(int studentId, int groupId, int weekIndex) =>
//         await service.GetStudentGradesByWeekAsync(studentId, groupId, weekIndex);
//     
//     [HttpPost]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> CreateGrade(CreateGradeDto createGradeDto) =>
//         await service.CreateGradeAsync(createGradeDto);
//     
//     [HttpDelete("{id}")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> DeleteGrade(int id) => 
//         await service.DeleteGradeAsync(id);
//     
//     [HttpPut]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> UpdateGrade(UpdateGradeDto gradeDto) => 
//         await service.EditGradeAsync(gradeDto);
// }