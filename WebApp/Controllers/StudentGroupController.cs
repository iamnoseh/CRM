using Domain.DTOs.StudentGroup;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StudentGroupController(IStudentGroupService studentGroupService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> CreateStudentGroup([FromBody] CreateStudentGroup request)
    {
        var result = await studentGroupService.CreateStudentGroupAsync(request);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpPut]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> UpdateStudentGroup(int id, [FromBody] UpdateStudentGroupDto request)
    {
        var result = await studentGroupService.UpdateStudentGroupAsync(id, request);
        return StatusCode(result.StatusCode, result);
    }
    [HttpDelete("student/{studentId}/group/{groupId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> RemoveStudentFromGroup(int studentId, int groupId)
    {
        var result = await studentGroupService.RemoveStudentFromGroup(studentId, groupId);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpDelete]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> DeleteStudentGroup(int id)
    {
        var result = await studentGroupService.DeleteStudentGroupAsync(id);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
    public async Task<ActionResult<Response<GetStudentGroupDto>>> GetStudentGroupById(int id)
    {
        var result = await studentGroupService.GetStudentGroupByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpGet]
    [Authorize]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
    public async Task<ActionResult<Response<List<GetStudentGroupDto>>>> GetAllStudentGroups()
    {
        var result = await studentGroupService.GetAllStudentGroupsAsync();
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpGet("paginated")]
    [Authorize]
    public async Task<ActionResult<PaginationResponse<List<GetStudentGroupDto>>>> GetStudentGroupsPaginated([FromQuery] StudentGroupFilter filter)
    {
        var result = await studentGroupService.GetStudentGroupsPaginated(filter);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpGet("student/{studentId}")]
    [Authorize]
    public async Task<ActionResult<Response<List<GetStudentGroupDto>>>> GetStudentGroupsByStudent(int studentId)
    {
        var result = await studentGroupService.GetStudentGroupsByStudentAsync(studentId);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpGet("group/{groupId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
    public async Task<ActionResult<Response<List<GetStudentGroupDto>>>> GetStudentGroupsByGroup(int groupId)
    {
        var result = await studentGroupService.GetStudentGroupsByGroupAsync(groupId);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpGet("group/{groupId}/left-students")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Mentor,Student")]
    public async Task<ActionResult<Response<List<LeftStudentDto>>>> GetLeftStudentsInGroup(int groupId)
    {
        var result = await studentGroupService.GetLeftStudentsInGroupAsync(groupId);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpPost("group/{groupId}/students")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> AddMultipleStudentsToGroup(int groupId, List<int> studentIds)
    {
        var result = await studentGroupService.AddMultipleStudentsToGroupAsync(groupId, studentIds);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpDelete("student/{studentId}/remove-from-all")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> RemoveStudentFromAllGroups(int studentId)
    {
        var result = await studentGroupService.RemoveStudentFromAllGroupsAsync(studentId);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpPut("left-student-from-group/{studentId}/{groupId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> LeftStudentFromGroup(int studentId, int groupId, [FromQuery] string leftReason)
    {
        var result = await studentGroupService.LeftStudentFromGroup(studentId, groupId, leftReason);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpPut("reverse-left-student-from-group/{studentId}/{groupId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> ReverseLeftStudentFromGroup(int studentId, int groupId)
    {
        var result = await studentGroupService.ReverseLeftStudentFromGroup(studentId, groupId);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpPut("transfer-student-group/{studentId}/{sourceGroupId}/{targetGroupId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> TransferStudentGroup(int studentId, int sourceGroupId, int targetGroupId)
    {
        var result = await studentGroupService.TransferStudentGroup(studentId, sourceGroupId, targetGroupId);
        return StatusCode(result.StatusCode, result);
    }
}