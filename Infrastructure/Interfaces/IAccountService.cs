using Domain.DTOs.Account;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IAccountService
{
    Task<Response<string>> Login(LoginDto login);
    Task<Response<string>> RemoveRoleFromUser(RoleDto userRole);
    Task<Response<string>> AddRoleToUser(RoleDto userRole);
    Task<Response<string>> ChangePassword(ChangePasswordDto passwordDto);
    
    // OTP-based password reset
    Task<Response<string>> SendOtp(SendOtpDto sendOtpDto);
    Task<Response<VerifyOtpResponseDto>> VerifyOtp(VerifyOtpDto verifyOtpDto);
    Task<Response<string>> ResetPasswordWithOtp(ResetPasswordDto resetPasswordDto);
}