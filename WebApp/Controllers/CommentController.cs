using Domain.DTOs.Comment;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentController(ICommentService commentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Response<List<GetCommentDto>>>> GetAllComments()
    {
        var response = await commentService.GetComments();
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Response<GetCommentDto>>> GetCommentById(int id)
    {
        var response = await commentService.GetCommentById(id);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<Response<List<GetCommentDto>>>> GetCommentsByStudent(int studentId)
    {
        var response = await commentService.GetCommentsByStudent(studentId);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<Response<List<GetCommentDto>>>> GetCommentsByGroup(int groupId)
    {
        var response = await commentService.GetCommentsByGroup(groupId);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("lesson/{lessonId}")]
    public async Task<ActionResult<Response<List<GetCommentDto>>>> GetCommentsByLesson(int lessonId)
    {
        var response = await commentService.GetCommentsByLesson(lessonId);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("type/{type}")]
    public async Task<ActionResult<Response<List<GetCommentDto>>>> GetCommentsByType(CommentType type)
    {
        var response = await commentService.GetCommentsByType(type);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("private/{authorId}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<List<GetCommentDto>>>> GetPrivateComments(int authorId)
    {
        var response = await commentService.GetPrivateComments(authorId);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> CreateComment([FromBody] CreateCommentDto createCommentDto)
    {
        var response = await commentService.CreateComment(createCommentDto);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPut]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> UpdateComment([FromBody] UpdateCommentDto updateCommentDto)
    {
        var response = await commentService.UpdateComment(updateCommentDto);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> DeleteComment(int id)
    {
        var response = await commentService.DeleteComment(id);
        return StatusCode(response.StatusCode, response);
    }
}