using Domain.DTOs.Mentor;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MentorController(IMentorService mentorService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Response<List<GetMentorDto>>>> GetMentors()
    {
        var response = await mentorService.GetMentors();
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<GetMentorDto>>> GetMentorById(int id)
    {
        var response = await mentorService.GetMentorByIdAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpGet("paginated")]
    public async Task<ActionResult<PaginationResponse<List<GetMentorDto>>>> GetMentorsPagination([FromQuery] MentorFilter filter)
    {
        var response = await mentorService.GetMentorsPagination(filter);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Response<string>>> CreateMentor([FromForm] CreateMentorDto createMentorDto)
    {
        var response = await mentorService.CreateMentorAsync(createMentorDto);
        return StatusCode((int)response.StatusCode, response);
    }


    [HttpPut]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> UpdateMentor(int id, [FromForm] UpdateMentorDto updateMentorDto)
    {
        var response = await mentorService.UpdateMentorAsync(id, updateMentorDto);
        return StatusCode((int)response.StatusCode, response);
    }


    [HttpDelete]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Response<string>>> DeleteMentor(int id)
    {
        var response = await mentorService.DeleteMentorAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }


    [HttpPut("profile-image")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> UpdateProfileImage(int id, IFormFile profileImage)
    {
        var response = await mentorService.UpdateUserProfileImageAsync(id, profileImage);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpGet("by-group/{groupId}")]
    public async Task<ActionResult<Response<List<GetMentorDto>>>> GetMentorsByGroup(int groupId)
    {
        var response = await mentorService.GetMentorsByGroupAsync(groupId);
        return StatusCode((int)response.StatusCode, response);
    }

    
    [HttpGet("by-course/{courseId}")]
    public async Task<ActionResult<Response<List<GetMentorDto>>>> GetMentorsByCourse(int courseId)
    {
        var response = await mentorService.GetMentorsByCourseAsync(courseId);
        return StatusCode((int)response.StatusCode, response);
    }
}