using Domain.DTOs.Student;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentController (IStudentService service) : ControllerBase
{
    [HttpGet]
    public async Task<Response<List<GetStudentDto>>> GetAllStudents() =>
        await service.GetStudents();

    [HttpGet("{id}")]
    public async Task<Response<GetStudentDto>> GetStudentById(int id ) => 
        await service.GetStudentByIdAsync(id );

    [HttpGet("filter")]
    public async Task<PaginationResponse<List<GetStudentDto>>> 
        GetStudentsPagination([FromQuery] StudentFilter filter ) =>
        await service.GetStudentsPagination(filter );

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<Response<string>> CreateStudent([FromForm] CreateStudentDto student) => 
        await service.CreateStudentAsync(student);

    [HttpPut("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Student}")]
    public async Task<Response<string>> UpdateStudent(int id, [FromForm] UpdateStudentDto dto) =>
        await service.UpdateStudentAsync(id, dto);
        
    [HttpPut("profile/{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Student}")]
    public async Task<Response<string>> UpdateStudentProfile(int id, IFormFile photo) =>
        await service.UpdateUserProfileImageAsync(id, photo);

    [HttpDelete("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<Response<string>> DeleteStudent(int id) =>
        await service.DeleteStudentAsync(id);


    [HttpGet("details/{id}")]
    public async Task<Response<GetStudentDetailedDto>> GetStudentDetailed(int id ) =>
        await service.GetStudentDetailedAsync(id );
}