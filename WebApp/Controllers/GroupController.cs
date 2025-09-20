using Domain.DTOs.Group;
using Domain.Filters;
using Infrastructure.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
public class GroupController(IGroupService groupService, DataContext context) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
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
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
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
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Student")]
    public async Task<IActionResult> GetGroupsByStudent(int studentId)
    {
        var roles = User?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (roles.Contains("Student"))
        {
            var idStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out var selfId))
                return Unauthorized("Invalid token: missing identifier");
            var respSelf = await groupService.GetGroupsByStudentIdAsync(selfId);
            return StatusCode(respSelf.StatusCode, respSelf);
        }
        var response = await groupService.GetGroupsByStudentIdAsync(studentId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("by-mentor/{mentorId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor")]
    public async Task<IActionResult> GetGroupsByMentor(int mentorId)
    {
        var roles = User?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (roles.Contains("Mentor"))
        {
            var idStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out var selfId))
                return Unauthorized("Invalid token: missing identifier");
            var respSelf = await groupService.GetGroupsByMentorIdAsync(selfId);
            return StatusCode(respSelf.StatusCode, respSelf);
        }
        var response = await groupService.GetGroupsByMentorIdAsync(mentorId);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyGroups()
    {
        var principalType = User?.FindFirst("PrincipalType")?.Value;
        var idStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out var principalId))
            return Unauthorized("Invalid token: missing identifier");

        if (string.Equals(principalType, "Student", StringComparison.OrdinalIgnoreCase))
        {
            var resp = await groupService.GetGroupsByStudentIdAsync(principalId);
            return StatusCode(resp.StatusCode, resp);
        }
        if (string.Equals(principalType, "Mentor", StringComparison.OrdinalIgnoreCase))
        {
            var resp = await groupService.GetGroupsByMentorIdAsync(principalId);
            return StatusCode(resp.StatusCode, resp);
        }

        return BadRequest("Unsupported principal type");
    }

    [HttpGet("paginated")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
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
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
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
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
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
