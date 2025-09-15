using Domain.DTOs.MentorGroup;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MentorGroupController(IMentorGroupService mentorGroupService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Mentor")]
    public async Task<ActionResult<Response<string>>> CreateMentorGroup([FromBody] CreateMentorGroupDto request)
    {
        var result = await mentorGroupService.CreateMentorGroupAsync(request);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpPut]
    [Authorize(Roles = "Admin,Manager,Mentor")]
    public async Task<ActionResult<Response<string>>> UpdateMentorGroup(int id, [FromBody] UpdateMentorGroupDto request)
    {
        var result = await mentorGroupService.UpdateMentorGroupAsync(id, request);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpDelete]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<Response<string>>> DeleteMentorGroup(int id)
    {
        var result = await mentorGroupService.DeleteMentorGroupAsync(id);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<Response<GetMentorGroupDto>>> GetMentorGroupById(int id)
    {
        var result = await mentorGroupService.GetMentorGroupByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<Response<List<GetMentorGroupDto>>>> GetAllMentorGroups()
    {
        var result = await mentorGroupService.GetAllMentorGroupsAsync();
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpGet("paginated")]
    [Authorize]
    public async Task<ActionResult<PaginationResponse<List<GetMentorGroupDto>>>> GetMentorGroupsPaginated([FromQuery] BaseFilter filter)
    {
        var result = await mentorGroupService.GetMentorGroupsPaginated(filter);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpGet("mentor/{mentorId}")]
    [Authorize]
    public async Task<ActionResult<Response<List<GetMentorGroupDto>>>> GetMentorGroupsByMentor(int mentorId)
    {
        var result = await mentorGroupService.GetMentorGroupsByMentorAsync(mentorId);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpGet("group/{groupId}")]
    [Authorize]
    public async Task<ActionResult<Response<List<GetMentorGroupDto>>>> GetMentorGroupsByGroup(int groupId)
    {
        var result = await mentorGroupService.GetMentorGroupsByGroupAsync(groupId);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpPost("group/{groupId}/mentors")]
    [Authorize(Roles = "Admin,Manager,Mentor")]
    public async Task<ActionResult<Response<string>>> AddMultipleMentorsToGroup(int groupId, [FromBody] List<int> mentorIds)
    {
        var result = await mentorGroupService.AddMultipleMentorsToGroupAsync(groupId, mentorIds);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpDelete("mentor/{mentorId}/remove-from-all")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<Response<string>>> RemoveMentorFromAllGroups(int mentorId)
    {
        var result = await mentorGroupService.RemoveMentorFromAllGroupsAsync(mentorId);
        return StatusCode(result.StatusCode, result);
    }
}
