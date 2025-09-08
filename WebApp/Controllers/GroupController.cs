using Domain.DTOs.Group;
using Domain.Filters;
using Infrastructure.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupController(IGroupService groupService, DataContext context) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromForm]CreateGroupDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await groupService.CreateGroupAsync(createDto);
        
        return response.StatusCode switch
        {
            201 => Created($"/api/group", response),
            400 => BadRequest(response),
            404 => NotFound(response),
            409 => Conflict(response), 
            _ => StatusCode(response.StatusCode, response)
        };
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGroup(int id)
    {
        var response = await groupService.GetGroupByIdAsync(id);
        
        return response.StatusCode switch
        {
            200 => Ok(response),
            404 => NotFound(response),
            _ => StatusCode(response.StatusCode, response)
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetGroups()
    {
        var response = await groupService.GetGroups();
        
        return response.StatusCode switch
        {
            200 => Ok(response),
            _ => StatusCode(response.StatusCode, response)
        };
    }

    [HttpGet("by-student/{studentId}")]
    public async Task<IActionResult> GetGroupsByStudent(int studentId)
    {
        var response = await groupService.GetGroupsByStudentIdAsync(studentId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("by-mentor/{mentorId}")]
    public async Task<IActionResult> GetGroupsByMentor(int mentorId)
    {
        var response = await groupService.GetGroupsByMentorIdAsync(mentorId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("paginated")]
    public async Task<IActionResult> GetGroupsPaginated([FromQuery] GroupFilter filter)
    {
        var response = await groupService.GetGroupPaginated(filter);
        
        return response.StatusCode switch
        {
            200 => Ok(response),
            _ => StatusCode(response.StatusCode, response)
        };
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGroup(int id, [FromForm] UpdateGroupDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var response = await groupService.UpdateGroupAsync(id, updateDto);
        
        return response.StatusCode switch
        {
            200 => Ok(response),
            400 => BadRequest(response),
            404 => NotFound(response),
            409 => Conflict(response),
            _ => StatusCode(response.StatusCode, response)
        };
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var response = await groupService.DeleteGroupAsync(id);
        
        return response.StatusCode switch
        {
            200 => Ok(response),
            404 => NotFound(response),
            _ => StatusCode(response.StatusCode, response)
        };
    }

    
    
} 