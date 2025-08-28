using Domain.DTOs.Mentor;
using Domain.DTOs.Student;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MentorController(IMentorService mentorService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<List<GetMentorDto>>>> GetMentors()
    {
        var response = await mentorService.GetMentors();
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Teacher")]
    public async Task<ActionResult<Response<GetMentorDto>>> GetMentorById(int id)
    {
        var response = await mentorService.GetMentorByIdAsync(id);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("paginated")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<PaginationResponse<List<GetMentorDto>>>> GetMentorsPagination([FromQuery] MentorFilter filter)
    {
        var response = await mentorService.GetMentorsPagination(filter);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("simple")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<PaginationResponse<List<GetSimpleDto>>>> GetSimpleMentorPagination()
    {
        var response = await mentorService.GetSimpleMentorPagination();
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> CreateMentor([FromForm] CreateMentorDto createMentorDto)
    {
        var response = await mentorService.CreateMentorAsync(createMentorDto);
        return StatusCode(response.StatusCode, response);
    }


    [HttpPut]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> UpdateMentor(int id, [FromForm] UpdateMentorDto updateMentorDto)
    {
        var response = await mentorService.UpdateMentorAsync(id, updateMentorDto);
        return StatusCode(response.StatusCode, response);
    }


    [HttpDelete]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> DeleteMentor(int id)
    {
        var response = await mentorService.DeleteMentorAsync(id);
        return StatusCode(response.StatusCode, response);
    }


    [HttpPut("profile-image")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> UpdateProfileImage(int id, IFormFile profileImage)
    {
        var response = await mentorService.UpdateUserProfileImageAsync(id, profileImage);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("by-group/{groupId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<List<GetMentorDto>>>> GetMentorsByGroup(int groupId)
    {
        var response = await mentorService.GetMentorsByGroupAsync(groupId);
        return StatusCode(response.StatusCode, response);
    }

    
    [HttpGet("by-course/{courseId}")]
    public async Task<ActionResult<Response<List<GetMentorDto>>>> GetMentorsByCourse(int courseId)
    {
        var response = await mentorService.GetMentorsByCourseAsync(courseId);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPut("document")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> UpdateMentorDocument(int mentorId, IFormFile documentFile)
    {
        var response = await mentorService.UpdateMentorDocumentAsync(mentorId, documentFile);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("document/{mentorId}")]
    [Authorize]
    public async Task<IActionResult> GetMentorDocument(int mentorId)
    {
        var response = await mentorService.GetMentorDocument(mentorId);
        
        if (response.StatusCode != (int)System.Net.HttpStatusCode.OK)
            return StatusCode((int)response.StatusCode, response);
        
        var mentor = await mentorService.GetMentorByIdAsync(mentorId);
        string contentType = "application/octet-stream";
        string fileName = "document";
        
        if (mentor.StatusCode == (int)System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(mentor.Data.Document))
        {
            string extension = Path.GetExtension(mentor.Data.Document).ToLowerInvariant();
            fileName = $"document_{mentorId}{extension}";
            
            // Определяем правильный MIME-тип на основе расширения
            contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
        
        return File(response.Data, contentType, fileName);
    }

    [HttpPut("payment-status")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<string>>> UpdateMentorPaymentStatus([FromBody] UpdateMentorPaymentStatusDto dto)
    {
        if (dto == null)
            return BadRequest(new Response<string>(System.Net.HttpStatusCode.BadRequest, "Маълумот нодуруст"));
        if (dto.MentorId <= 0)
            return BadRequest(new Response<string>(System.Net.HttpStatusCode.BadRequest, "MentorId нодуруст аст"));
        var response = await mentorService.UpdateMentorPaymentStatusAsync(dto.MentorId, dto.Status);
        return StatusCode(response.StatusCode, response);
    }
    
}