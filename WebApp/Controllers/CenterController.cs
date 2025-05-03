using Domain.DTOs.Center;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CenterController(ICenterService centerService) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<Response<List<GetCenterDto>>>> GetCenters()
    {
        var response = await centerService.GetCenters();
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<GetCenterDto>>> GetCenterById(int id)
    {
        var response = await centerService.GetCenterByIdAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }
    
    [HttpGet("paginated")]
    public async Task<ActionResult<PaginationResponse<List<GetCenterDto>>>> GetCentersPaginated([FromQuery] CenterFilter filter)
    {
        var response = await centerService.GetCentersPaginated(filter);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Response<string>>> CreateCenter([FromForm] CreateCenterDto createCenterDto)
    {
        var response = await centerService.CreateCenterAsync(createCenterDto);
        return StatusCode((int)response.StatusCode, response);
    }


    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Response<string>>> UpdateCenter(int id, [FromForm] UpdateCenterDto updateCenterDto)
    {
        var response = await centerService.UpdateCenterAsync(id, updateCenterDto);
        return StatusCode((int)response.StatusCode, response);
    }


    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Response<string>>> DeleteCenter(int id)
    {
        var response = await centerService.DeleteCenterAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpGet("{id}/groups")]
    public async Task<ActionResult<Response<List<GetCenterGroupsDto>>>> GetCenterGroups(int id)
    {
        var response = await centerService.GetCenterGroupsAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }


    [HttpGet("{id}/students")]
    public async Task<ActionResult<Response<List<GetCenterStudentsDto>>>> GetCenterStudents(int id)
    {
        var response = await centerService.GetCenterStudentsAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpGet("{id}/mentors")]
    public async Task<ActionResult<Response<List<GetCenterMentorsDto>>>> GetCenterMentors(int id)
    {
        var response = await centerService.GetCenterMentorsAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpGet("{id}/courses")]
    public async Task<ActionResult<Response<List<GetCenterCoursesDto>>>> GetCenterCourses(int id)
    {
        var response = await centerService.GetCenterCoursesAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }


    [HttpGet("{id}/statistics")]
    public async Task<ActionResult<Response<CenterStatisticsDto>>> GetCenterStatistics(int id)
    {
        var response = await centerService.GetCenterStatisticsAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }
}
