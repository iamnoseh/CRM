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
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> CreateStudentGroup([FromBody] CreateStudentGroup request)
    {
        var result = await studentGroupService.CreateStudentGroupAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpPut]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> UpdateStudentGroup(int id, [FromBody] UpdateStudentGroupDto request)
    {
        var result = await studentGroupService.UpdateStudentGroupAsync(id, request);
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpDelete]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> DeleteStudentGroup(int id)
    {
        var result = await studentGroupService.DeleteStudentGroupAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<Response<GetStudentGroupDto>>> GetStudentGroupById(int id)
    {
        var result = await studentGroupService.GetStudentGroupByIdAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<Response<List<GetStudentGroupDto>>>> GetAllStudentGroups()
    {
        var result = await studentGroupService.GetAllStudentGroupsAsync();
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpGet("paginated")]
    [Authorize]
    public async Task<ActionResult<PaginationResponse<List<GetStudentGroupDto>>>> GetStudentGroupsPaginated([FromQuery] BaseFilter filter)
    {
        var result = await studentGroupService.GetStudentGroupsPaginated(filter);
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpGet("student/{studentId}")]
    [Authorize]
    public async Task<ActionResult<Response<List<GetStudentGroupDto>>>> GetStudentGroupsByStudent(int studentId)
    {
        var result = await studentGroupService.GetStudentGroupsByStudentAsync(studentId);
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpGet("group/{groupId}")]
    [Authorize]
    public async Task<ActionResult<Response<List<GetStudentGroupDto>>>> GetStudentGroupsByGroup(int groupId)
    {
        var result = await studentGroupService.GetStudentGroupsByGroupAsync(groupId);
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpPost("group/{groupId}/students")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> AddMultipleStudentsToGroup(int groupId, List<int> studentIds)
    {
        var result = await studentGroupService.AddMultipleStudentsToGroupAsync(groupId, studentIds);
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpDelete("student/{studentId}/remove-from-all")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> RemoveStudentFromAllGroups(int studentId)
    {
        var result = await studentGroupService.RemoveStudentFromAllGroupsAsync(studentId);
        return StatusCode((int)result.StatusCode, result);
    }
}