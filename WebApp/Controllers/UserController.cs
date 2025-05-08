using Domain.DTOs.User;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<PaginationResponse<List<GetUserDto>>>> GetAllUsers([FromQuery] UserFilter filter)
    {
        var result = await service.GetUsersAsync(filter);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<Response<GetUserDto>>> GetUserById(int id)
    {
        var result = await service.GetUserByIdAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }
        
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<Response<GetUserDto>>> GetCurrentUser()
    {
        var result = await service.GetCurrentUserAsync();
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpGet("search")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<Response<List<GetUserDto>>>> SearchUsers([FromQuery] string searchTerm)
    {
        var result = await service.SearchUsersAsync(searchTerm);
        return StatusCode((int)result.StatusCode, result);
    }
    
    [HttpGet("role/{role}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<Response<List<GetUserDto>>>> GetUsersByRole(string role)
    {
        var result = await service.GetUsersByRoleAsync(role);
        return StatusCode((int)result.StatusCode, result);
    }
    //
    // [HttpGet("{userId}/activity")]
    // [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    // public async Task<ActionResult<Response<UserActivityDto>>> GetUserActivity(int userId)
    // {
    //     var result = await service.GetUserActivityAsync(userId);
    //     return StatusCode((int)result.StatusCode, result);
    // }
}