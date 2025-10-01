using Domain.DTOs.Account;
using Domain.Entities;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(IAccountService service) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<Response<string>> Register([FromForm] RegisterDto request) =>
        await service.Register(request);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<Response<string>> Login(LoginDto request) =>
        await service.Login(request);

    [HttpPost("add-role-to-user")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<Response<string>> AddRoleToUser(RoleDto request) =>
        await service.AddRoleToUser(request);

    [HttpDelete("remove-role-from-user")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<Response<string>> RemoveRoleFromUser(RoleDto request) =>
        await service.RemoveRoleFromUser(request);
    
    [HttpPut("change-password")]
    [Authorize]
    public async Task<Response<string>> ChangePassword([FromForm] ChangePasswordDto changePasswordDto) =>
        await service.ChangePassword(changePasswordDto);

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<Response<string>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto) =>
        await service.ForgotPasswordCodeGenerator(forgotPasswordDto);

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<Response<string>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto) =>
        await service.ResetPassword(resetPasswordDto);
}