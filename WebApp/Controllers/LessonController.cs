// using Domain.DTOs.Lesson;
// using Domain.Entities;
// using Domain.Responses;
// using Infrastructure.Interfaces;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
//
// namespace WebApp.Controllers;
// [ApiController]
// [Route("api/[controller]")]
// public class LessonController (ILessonService service) : ControllerBase
// {
//     [HttpGet]
//     public async Task<Response<List<GetLessonDto>>> GetAllLessons() => 
//         await service.GetLessons();
//
//     [HttpGet("{id}")]
//     public async Task<Response<GetLessonDto>> GetLessonById(int id) => 
//         await service.GetLessonById(id);
//     
//     [HttpGet("group/{groupId}")]
//     public async Task<Response<List<GetLessonDto>>> GetLessonsByGroup(int groupId) =>
//         await service.GetLessonsByGroup(groupId);
//         
//     [HttpGet("group/{groupId}/week/{weekIndex}")]
//     public async Task<Response<List<GetLessonDto>>> GetLessonsByWeek(int groupId, int weekIndex) =>
//         await service.GetLessonsByWeek(groupId, weekIndex);
//         
//     [HttpGet("group/{groupId}/week/{weekIndex}/day/{dayOfWeekIndex}")]
//     public async Task<Response<List<GetLessonDto>>> GetLessonsByDay(int groupId, int weekIndex, int dayOfWeekIndex) =>
//         await service.GetLessonsByDay(groupId, weekIndex, dayOfWeekIndex);
//     
//     [HttpPost]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> CreateLesson(CreateLessonDto createLesson) =>
//         await service.CreateLesson(createLesson);
//         
//     [HttpPost("weekly")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> CreateWeeklyLessons(int groupId, int weekIndex, [FromBody] DateTimeOffset startDate) =>
//         await service.CreateWeeklyLessons(groupId, weekIndex, startDate);
//     
//     [HttpDelete("{id}")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> DeleteLesson(int id) => 
//         await service.DeleteLesson(id);
//     
//     [HttpPut]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> UpdateLesson(UpdateLessonDto lessonDto) => 
//         await service.UpdateLesson(lessonDto);
// }