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
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto request)
    {
        var response = await service.Login(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("add-role-to-user")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<IActionResult> AddRoleToUser(RoleDto request)
    {
        var response = await service.AddRoleToUser(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("remove-role-from-user")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin},{Roles.Manager}")]
    public async Task<IActionResult> RemoveRoleFromUser(RoleDto request)
    {
        var response = await service.RemoveRoleFromUser(request);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordDto changePasswordDto)
    {
        var response = await service.ChangePassword(changePasswordDto);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("send-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpDto sendOtpDto)
    {
        var response = await service.SendOtp(sendOtpDto);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto verifyOtpDto)
    {
        var response = await service.VerifyOtp(verifyOtpDto);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
    {
        var response = await service.ResetPasswordWithOtp(resetPasswordDto);
        return StatusCode(response.StatusCode, response);
    }
}