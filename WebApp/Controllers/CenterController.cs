using Domain.DTOs.Center;
using Domain.DTOs.Course;
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
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<List<GetCenterDto>>>> GetCenters()
    {
        var response = await centerService.GetCenters();
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<GetCenterDto>>> GetCenterById(int id)
    {
        var response = await centerService.GetCenterByIdAsync(id);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpGet("paginated")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<PaginationResponse<List<GetCenterDto>>>> GetCentersPaginated([FromQuery] CenterFilter filter)
    {
        var response = await centerService.GetCentersPaginated(filter);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("simple-paginated")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager,Student")]
    public async Task<ActionResult<PaginationResponse<List<GetCenterSimpleDto>>>> GetCentersSimplePaginated(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 30)
    {
        var response = await centerService.GetCentersSimplePaginated(page, pageSize);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<Response<string>>> CreateCenter([FromForm] CreateCenterDto createCenterDto)
    {
        var response = await centerService.CreateCenterAsync(createCenterDto);
        return StatusCode(response.StatusCode, response);
    }


    [HttpPut]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<Response<string>>> UpdateCenter(int id, [FromForm] UpdateCenterDto updateCenterDto)
    {
        var response = await centerService.UpdateCenterAsync(id, updateCenterDto);
        return StatusCode(response.StatusCode, response);
    }


    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<Response<string>>> DeleteCenter(int id)
    {
        var response = await centerService.DeleteCenterAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}/groups")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<List<GetCenterGroupsDto>>>> GetCenterGroups(int id)
    {
        var response = await centerService.GetCenterGroupsAsync(id);
        return StatusCode(response.StatusCode, response);
    }


    [HttpGet("{id}/students")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<List<GetCenterStudentsDto>>>> GetCenterStudents(int id)
    {
        var response = await centerService.GetCenterStudentsAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}/mentors")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<List<GetCenterMentorsDto>>>> GetCenterMentors(int id)
    {
        var response = await centerService.GetCenterMentorsAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}/courses")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<List<GetCenterCoursesDto>>>> GetCenterCourses(int id)
    {
        var response = await centerService.GetCenterCoursesAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{centerId}/courses-with-stats")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<Response<List<GetCourseWithStatsDto>>> GetCenterCoursesWithStats(int centerId)
        => await centerService.GetCenterCoursesWithStatsAsync(centerId);

    [HttpGet("{id}/statistics")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Response<CenterStatisticsDto>>> GetCenterStatistics(int id)
    {
        var response = await centerService.GetCenterStatisticsAsync(id);
        return StatusCode(response.StatusCode, response);
    }
}
