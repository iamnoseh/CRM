// using Domain.DTOs.Comment;
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
// public class 
//     CommentController(ICommentService service) : ControllerBase
// {
//     [HttpGet]
//     public async Task<Response<List<GetCommentDto>>> GetAllComments() => 
//         await service.GetComments();
//
//     [HttpGet("{id}")]
//     public async Task<Response<GetCommentDto>> GetCommentById(int id) => 
//         await service.GetCommentById(id);
//     
//     [HttpGet("student/{studentId}")]
//     public async Task<Response<List<GetCommentDto>>> GetCommentsByStudent(int studentId) =>
//         await service.GetCommentsByStudent(studentId);
//         
//     [HttpGet("group/{groupId}")]
//     public async Task<Response<List<GetCommentDto>>> GetCommentsByGroup(int groupId) =>
//         await service.GetCommentsByGroup(groupId);
//         
//     [HttpGet("lesson/{lessonId}")]
//     public async Task<Response<List<GetCommentDto>>> GetCommentsByLesson(int lessonId) =>
//         await service.GetCommentsByLesson(lessonId);
//     
//     [HttpPost]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> CreateComment(CreateCommentDto createComment) =>
//         await service.CreateComment(createComment);
//     
//     [HttpDelete("{id}")]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> DeleteComment(int id) => 
//         await service.DeleteComment(id);
//     
//     [HttpPut]
//     [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.SuperAdmin}")]
//     public async Task<Response<string>> UpdateComment(UpdateCommentDto commentDto) => 
//         await service.UpdateComment(commentDto);
// } 