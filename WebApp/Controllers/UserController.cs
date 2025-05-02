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
    public async Task<PaginationResponse<List<GetUserDto>>> GetAllUsers([FromQuery] UserFilter filter) =>
        await service.GetUsersAsync(filter);

    [HttpGet("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<Response<GetUserDto>> GetUserById(int id) => 
        await service.GetUserByIdAsync(id);
        
    [HttpGet("me")]
    [Authorize]
    public async Task<Response<GetUserDto>> GetCurrentUser() => 
        await service.GetCurrentUserAsync();
}