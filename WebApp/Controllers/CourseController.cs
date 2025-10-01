using Domain.DTOs.Course;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourseController (ICourseService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<Response<List<GetCourseDto>>> GetAllCourse() =>
        await service.GetCourses();
        
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<Response<GetCourseDto>> GetCourseById(int id) => 
        await service.GetCourseByIdAsync(id );

    [HttpGet("filter")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<PaginationResponse<List<GetCourseDto>>> 
        GetCoursesPagination([FromQuery] CourseFilter filter) =>
        await service.GetCoursesPagination(filter );
        
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<Response<string>> CreateCourse([FromForm] CreateCourseDto course) => 
        await service.CreateCourseAsync(course);
        
    [HttpPut]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<Response<string>> UpdateCourse(int id, [FromForm] UpdateCourseDto dto) =>
        await service.UpdateCourseAsync(dto);

    [HttpDelete("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<Response<string>> DeleteCourse(int id) =>
        await service.DeleteCourseAsync(id);

    [HttpGet("{id}/groups")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<Response<GetCourseGroupsDto>> GetCourseGroupsAndCount(int id)
        => await service.GetCourseGroupsAndCountAsync(id);

    [HttpGet("simple")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<PaginationResponse<List<GetSimpleCourseDto>>> GetSimpleCourses([FromQuery] BaseFilter filter) =>
        await service.GetSimpleCourses(filter);
}