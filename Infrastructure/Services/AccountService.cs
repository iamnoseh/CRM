using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Domain.DTOs.Account;
using Domain.Entities;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Infrastructure.Constants;
using Infrastructure.Services.EmailService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public class AccountService(
    UserManager<User> userManager,
    RoleManager<IdentityRole<int>> roleManager,
    IConfiguration configuration,
    DataContext context,
    IEmailService emailService,
    IOsonSmsService osonSmsService,
    IHttpContextAccessor httpContextAccessor) : IAccountService
{
    #region Login

    public async Task<Response<string>> Login(LoginDto login)
    {
        var user = await userManager.FindByNameAsync(login.Username);
        if (user == null)
            return new Response<string>(HttpStatusCode.NotFound, Messages.Account.InvalidCredentials);

        var isPasswordValid = await userManager.CheckPasswordAsync(user, login.Password);
        if (!isPasswordValid)
            return new Response<string>(HttpStatusCode.BadRequest, Messages.Account.InvalidCredentials);

        var token = await GenerateJwtToken(user);
        return new Response<string>(token) { Message = Messages.Account.LoginSuccess };
    }

    #endregion

    #region AddRoleToUser

    public async Task<Response<string>> AddRoleToUser(RoleDto userRole)
    {
        var user = await userManager.FindByIdAsync(userRole.UserId);
        if (user == null)
            return new Response<string>(HttpStatusCode.NotFound, Messages.User.NotFound);

        if (!await roleManager.RoleExistsAsync(userRole.RoleName))
            return new Response<string>(HttpStatusCode.BadRequest, Messages.Account.RoleNotExists);

        var result = await userManager.AddToRoleAsync(user, userRole.RoleName);
        if (!result.Succeeded)
            return new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(result));

        return new Response<string>(HttpStatusCode.OK, Messages.Account.RoleAdded);
    }

    #endregion

    #region GetUserRoles

    public async Task<Response<List<string>>> GetUserRoles(int userId)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return new Response<List<string>>(HttpStatusCode.NotFound, Messages.User.NotFound);

            var roles = await userManager.GetRolesAsync(user);
            return new Response<List<string>>(roles.ToList()) { Message = Messages.Account.RolesFetched };
        }
        catch (Exception ex)
        {
            return new Response<List<string>>(HttpStatusCode.InternalServerError, string.Format(Messages.Account.RolesFetchError, ex.Message));
        }
    }

    #endregion

    #region RemoveRoleFromUser

    public async Task<Response<string>> RemoveRoleFromUser(RoleDto userRole)
    {
        var user = await userManager.FindByIdAsync(userRole.UserId);
        if (user == null)
            return new Response<string>(HttpStatusCode.NotFound, Messages.User.NotFound);

        var result = await userManager.RemoveFromRoleAsync(user, userRole.RoleName);
        if (!result.Succeeded)
            return new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(result));

        return new Response<string>(HttpStatusCode.OK, Messages.Account.RoleRemoved);
    }

    #endregion

    #region GenerateJwtToken

    private async Task<string> GenerateJwtToken(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user), Messages.Account.UserCannotBeNull);

        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);
        var securityKey = new SymmetricSecurityKey(key);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var roles = await userManager.GetRolesAsync(user);
        var normalizedRoles = roles.Select(r => string.Equals(r, "Teacher", StringComparison.OrdinalIgnoreCase) ? "Mentor" : r)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        int principalId = user.Id;
        string principalType = "User";

        if (normalizedRoles.Contains("Student"))
        {
            var student = await context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student != null)
            {
                principalId = student.Id;
                principalType = "Student";
            }
        }
        else if (normalizedRoles.Contains("Mentor"))
        {
            var mentor = await context.Mentors.AsNoTracking().FirstOrDefaultAsync(m => m.UserId == user.Id);
            if (mentor != null)
            {
                principalId = mentor.Id;
                principalType = "Mentor";
            }
        }

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.NameId, principalId.ToString()),
            new Claim("UserId", user.Id.ToString()),
            new Claim("PrincipalType", principalType)
        };

        if (!string.IsNullOrEmpty(user.UserName))
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, user.UserName));

        if (!string.IsNullOrEmpty(user.FullName))
            claims.Add(new Claim("Fullname", user.FullName));

        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

        if (!string.IsNullOrEmpty(user.ProfileImagePath))
            claims.Add(new Claim(JwtRegisteredClaimNames.Picture, user.ProfileImagePath));
        else
            claims.Add(new Claim(JwtRegisteredClaimNames.Picture, "null"));

        if (normalizedRoles.Count > 0)
            claims.AddRange(normalizedRoles.Where(role => !string.IsNullOrEmpty(role)).Select(role => new Claim("role", role)));

        bool isSuperAdmin = normalizedRoles.Contains("SuperAdmin");
        if (!isSuperAdmin && user.CenterId.HasValue)
            claims.Add(new Claim("CenterId", user.CenterId.Value.ToString()));

        var expirationDays = int.TryParse(configuration["Jwt:ExpirationDays"], out var days) ? days : 3;
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expirationDays),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return tokenString;
    }

    #endregion

    #region ChangePassword

    public async Task<Response<string>> ChangePassword(ChangePasswordDto passwordDto)
    {
        try
        {
            var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst("UserId")?.Value
                              ?? httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? httpContextAccessor.HttpContext?.User.FindFirst("nameid")?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userIdFromClaim) || userIdFromClaim <= 0)
                return new Response<string>(HttpStatusCode.Unauthorized, Messages.User.UserNotAuthenticated);

            var existingUser = await userManager.Users.FirstOrDefaultAsync(x => x.Id == userIdFromClaim);
            if (existingUser == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.User.NotFound);

            var changePassResult = await userManager.ChangePasswordAsync(existingUser, passwordDto.OldPassword, passwordDto.Password);
            if (!changePassResult.Succeeded)
                return new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(changePassResult));

            return new Response<string>(HttpStatusCode.OK, Messages.Account.PasswordChanged);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Account.PasswordChangeError, ex.Message));
        }
    }

    #endregion

    #region SendOtp

    public async Task<Response<string>> SendOtp(SendOtpDto sendOtpDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sendOtpDto.Username))
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Account.UsernameRequired);

            var existingUser = await userManager.FindByNameAsync(sendOtpDto.Username);
            if (existingUser == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Account.UserNotFoundByUsername);

            var otpCode = new Random().Next(1000, 9999).ToString();

            existingUser.Code = otpCode;
            existingUser.CodeDate = DateTime.UtcNow;

            var result = await context.SaveChangesAsync();
            if (result <= 0)
                return new Response<string>(HttpStatusCode.InternalServerError, Messages.Account.OtpCreationError);

            if (!string.IsNullOrWhiteSpace(existingUser.PhoneNumber))
            {
                var smsMessage = $"Здравствуйте!\nВы запросили восстановление пароля.\nКод подтверждения: {otpCode}\nКод действителен только 3 минуты.\nKavsar Academy";
                await osonSmsService.SendSmsAsync(existingUser.PhoneNumber, smsMessage);
            }

            if (!string.IsNullOrWhiteSpace(existingUser.Email))
                await EmailHelper.SendResetPasswordCodeEmailAsync(emailService, existingUser.Email, otpCode);

            return new Response<string>(HttpStatusCode.OK, Messages.Account.OtpSent);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Account.OtpSendError, ex.Message));
        }
    }

    #endregion

    #region VerifyOtp

    public async Task<Response<VerifyOtpResponseDto>> VerifyOtp(VerifyOtpDto verifyOtpDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(verifyOtpDto.Username) || string.IsNullOrWhiteSpace(verifyOtpDto.OtpCode))
                return new Response<VerifyOtpResponseDto>(HttpStatusCode.BadRequest, Messages.Account.UsernameAndOtpRequired);

            var existingUser = await userManager.FindByNameAsync(verifyOtpDto.Username);
            if (existingUser == null)
                return new Response<VerifyOtpResponseDto>(HttpStatusCode.NotFound, Messages.Account.UserNotFoundByUsername);

            if (existingUser.Code != verifyOtpDto.OtpCode)
                return new Response<VerifyOtpResponseDto>(HttpStatusCode.BadRequest, Messages.Account.OtpInvalid);

            var timeElapsed = DateTime.UtcNow - existingUser.CodeDate;
            if (timeElapsed.TotalMinutes > 3)
                return new Response<VerifyOtpResponseDto>(HttpStatusCode.BadRequest, Messages.Account.OtpExpired);

            var resetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + "_" + existingUser.Id;
            existingUser.Code = $"VERIFIED_{resetToken}";
            existingUser.CodeDate = DateTime.UtcNow;
            await context.SaveChangesAsync();

            var responseData = new VerifyOtpResponseDto
            {
                ResetToken = resetToken,
                Message = Messages.Account.OtpVerified
            };

            return new Response<VerifyOtpResponseDto>(responseData) { Message = Messages.Account.OtpVerified };
        }
        catch (Exception ex)
        {
            return new Response<VerifyOtpResponseDto>(HttpStatusCode.InternalServerError, string.Format(Messages.Account.OtpVerifyError, ex.Message));
        }
    }

    #endregion

    #region ResetPasswordWithOtp

    public async Task<Response<string>> ResetPasswordWithOtp(ResetPasswordDto resetPasswordDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(resetPasswordDto.ResetToken) ||
                string.IsNullOrWhiteSpace(resetPasswordDto.NewPassword) || string.IsNullOrWhiteSpace(resetPasswordDto.ConfirmPassword))
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Account.TokenAndPasswordRequired);

            if (resetPasswordDto.NewPassword != resetPasswordDto.ConfirmPassword)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Account.PasswordsNotMatch);

            var tokenParts = resetPasswordDto.ResetToken.Split('_');
            if (tokenParts.Length != 2 || !int.TryParse(tokenParts[1], out var userId))
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Account.TokenInvalid);

            var existingUser = await userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (existingUser == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.User.NotFound);

            var expectedCode = $"VERIFIED_{resetPasswordDto.ResetToken}";
            if (existingUser.Code != expectedCode)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Account.TokenUsedOrInvalid);

            var timeElapsed = DateTime.UtcNow - existingUser.CodeDate;
            if (timeElapsed.TotalMinutes > 10)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Account.TokenExpired);

            var passwordResetToken = await userManager.GeneratePasswordResetTokenAsync(existingUser);
            var resetResult = await userManager.ResetPasswordAsync(existingUser, passwordResetToken, resetPasswordDto.NewPassword);
            if (!resetResult.Succeeded)
                return new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(resetResult));

            existingUser.Code = null;
            existingUser.CodeDate = default;
            await context.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.OK, Messages.Account.PasswordReset);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.Account.PasswordResetError, ex.Message));
        }
    }

    #endregion
}
