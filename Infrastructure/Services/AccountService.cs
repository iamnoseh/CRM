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
using Infrastructure.Services.EmailService;
using Infrastructure.Services.HashService;
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
    IHashService hashService,
    IOsonSmsService osonSmsService,
    string uploadPath,
    IHttpContextAccessor httpContextAccessor) : IAccountService
{
    public async Task<Response<string>> Login(LoginDto login)
    {
        var user = await userManager.FindByNameAsync(login.Username);
        if (user == null)
            return new Response<string>(HttpStatusCode.NotFound, "Номи корбар ё рамз нодуруст аст");

        var isPasswordValid = await userManager.CheckPasswordAsync(user, login.Password);
        if (!isPasswordValid)
            return new Response<string>(HttpStatusCode.BadRequest, "Номи корбар ё рамз нодуруст аст");

        var token = await GenerateJwtToken(user);
        return new Response<string>(token) { Message = "Воридшавӣ бо муваффақият анҷом ёфт" };
    }

    public async Task<Response<string>> AddRoleToUser(RoleDto userRole)
    {
        var user = await userManager.FindByIdAsync(userRole.UserId);
        if (user == null)
            return new Response<string>(HttpStatusCode.NotFound, "Корбар ёфт нашуд");

        if (!await roleManager.RoleExistsAsync(userRole.RoleName))
            return new Response<string>(HttpStatusCode.BadRequest, "Нақш вуҷуд надорад");

        var result = await userManager.AddToRoleAsync(user, userRole.RoleName);
        if (!result.Succeeded)
            return new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(result));

        return new Response<string>(HttpStatusCode.OK, "Нақш бо муваффақият илова карда шуд");
    }

    public async Task<Response<List<string>>> GetUserRoles(int userId)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return new Response<List<string>>(HttpStatusCode.NotFound, "Корбар ёфт нашуд");

            var roles = await userManager.GetRolesAsync(user);
            return new Response<List<string>>(roles.ToList()) { Message = "Нақшҳои корбар бо муваффақият гирифта шуданд" };
        }
        catch (Exception ex)
        {
            return new Response<List<string>>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми гирифтани нақшҳои корбар: {ex.Message}");
        }
    }

    public async Task<Response<string>> RemoveRoleFromUser(RoleDto userRole)
    {
        var user = await userManager.FindByIdAsync(userRole.UserId);
        if (user == null)
            return new Response<string>(HttpStatusCode.NotFound, "Корбар ёфт нашуд");

        var result = await userManager.RemoveFromRoleAsync(user, userRole.RoleName);
        if (!result.Succeeded)
            return new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(result));

        return new Response<string>(HttpStatusCode.OK, "Нақш бо муваффақият нест карда шуд");
    }

    private async Task<string> GenerateJwtToken(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "Корбар наметавонад null бошад");
        }

        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]);
        var securityKey = new SymmetricSecurityKey(key);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var roles = await userManager.GetRolesAsync(user);
        var normalizedRoles = roles?.Select(r => string.Equals(r, "Teacher", StringComparison.OrdinalIgnoreCase) ? "Mentor" : r)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

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
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, user.UserName));
        }

        if (!string.IsNullOrEmpty(user.FullName))
        {
            claims.Add(new Claim("Fullname", user.FullName));
        }

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        }

        if (!string.IsNullOrEmpty(user.ProfileImagePath))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Picture, user.ProfileImagePath));
        }
        else
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Picture, "null"));
        }

        if (normalizedRoles.Count > 0)
        {
            claims.AddRange(normalizedRoles.Where(role => !string.IsNullOrEmpty(role)).Select(role => new Claim("role", role)));
        }

        bool isSuperAdmin = normalizedRoles.Contains("SuperAdmin");
        if (!isSuperAdmin && user.CenterId.HasValue)
        {
            claims.Add(new Claim("CenterId", user.CenterId.Value.ToString()));
        }

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

    public async Task<Response<string>> ChangePassword(ChangePasswordDto passwordDto)
    {
        try
        {
            if (passwordDto == null)
                return new Response<string>(HttpStatusCode.BadRequest, "Маълумоти рамз нодуруст аст");
            
            var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("UserId")?.Value
                              ?? httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? httpContextAccessor.HttpContext?.User?.FindFirst("nameid")?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userIdFromClaim) || userIdFromClaim <= 0)
                return new Response<string>(HttpStatusCode.Unauthorized, "Истифодабаранда аутентификатсия нашудааст");

            var existingUser = await userManager.Users.FirstOrDefaultAsync(x => x.Id == userIdFromClaim);
            if (existingUser == null)
                return new Response<string>(HttpStatusCode.NotFound, "Корбар ёфт нашуд");

            var changePassResult = await userManager.ChangePasswordAsync(existingUser, passwordDto.OldPassword, passwordDto.Password);
            if (!changePassResult.Succeeded)
                return new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(changePassResult));

            return new Response<string>(HttpStatusCode.OK, "Рамз бо муваффақият иваз карда шуд");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми ивазкунии рамз: {ex.Message}");
        }
    }

    public async Task<Response<string>> SendOtp(SendOtpDto sendOtpDto)
    {
        try
        {
            if (sendOtpDto == null || string.IsNullOrWhiteSpace(sendOtpDto.Username))
                return new Response<string>(HttpStatusCode.BadRequest, "Номи корбар ҳатмист");
            var existingUser = await userManager.FindByNameAsync(sendOtpDto.Username);
            
            if (existingUser == null)
                return new Response<string>(HttpStatusCode.NotFound, "Корбари бо ин номи корбар ёфт нашуд");

            var otpCode = new Random().Next(1000, 9999).ToString();
            
            existingUser.Code = otpCode;
            existingUser.CodeDate = DateTime.UtcNow;

            var result = await context.SaveChangesAsync();
            if (result <= 0)
                return new Response<string>(HttpStatusCode.InternalServerError, "Хатогӣ ҳангоми сохтани рамзи тасдиқ");

            if (!string.IsNullOrWhiteSpace(existingUser.PhoneNumber))
            {
                var smsMessage = $"Салом!\nШумо дархости барқарорсозии рамзро пешниҳод кардед.\nРамзи тасдиқи : {otpCode}\nРамз танҳо 3 дақиқа эътибор дорад.\nKavsar Academy";
                await osonSmsService.SendSmsAsync(existingUser.PhoneNumber, smsMessage);
            }

            if (!string.IsNullOrWhiteSpace(existingUser.Email))
            {
                await EmailHelper.SendResetPasswordCodeEmailAsync(emailService, existingUser.Email, otpCode);
            }

            return new Response<string>(HttpStatusCode.OK, "Рамзи тасдиқи OTP ба телефон ва/ё email бо муваффақият фиристода шуд");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми фиристодани OTP: {ex.Message}");
        }
    }

    public async Task<Response<VerifyOtpResponseDto>> VerifyOtp(VerifyOtpDto verifyOtpDto)
    {
        try
        {
            if (verifyOtpDto == null || string.IsNullOrWhiteSpace(verifyOtpDto.Username) || string.IsNullOrWhiteSpace(verifyOtpDto.OtpCode))
                return new Response<VerifyOtpResponseDto>(HttpStatusCode.BadRequest, "Номи корбар ва рамзи тасдиқ ҳатмист");

            var existingUser = await userManager.FindByNameAsync(verifyOtpDto.Username);
            
            if (existingUser == null)
                return new Response<VerifyOtpResponseDto>(HttpStatusCode.NotFound, "Корбари бо ин номи корбар ёфт нашуд");

            if (existingUser.Code != verifyOtpDto.OtpCode)
                return new Response<VerifyOtpResponseDto>(HttpStatusCode.BadRequest, "Рамзи тасдиқи OTP нодуруст аст");

            var timeElapsed = DateTime.UtcNow - existingUser.CodeDate;
            if (timeElapsed.TotalMinutes > 3)
                return new Response<VerifyOtpResponseDto>(HttpStatusCode.BadRequest, "Мӯҳлати рамзи тасдиқи OTP гузаштааст (3 дақиқа)");

            var resetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + "_" + existingUser.Id;
            
            existingUser.Code = $"VERIFIED_{resetToken}";
            existingUser.CodeDate = DateTime.UtcNow; 
            await context.SaveChangesAsync();

            var responseData = new VerifyOtpResponseDto
            {
                ResetToken = resetToken,
                Message = "Рамзи тасдиқи OTP дуруст аст. Токен барои барқарорсозии рамз дода шуд."
            };

            return new Response<VerifyOtpResponseDto>(responseData) 
            { 
                Message = "Рамзи тасдиқи OTP дуруст аст" 
            };
        }
        catch (Exception ex)
        {
            return new Response<VerifyOtpResponseDto>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми тасдиқи OTP: {ex.Message}");
        }
    }

    public async Task<Response<string>> ResetPasswordWithOtp(ResetPasswordDto resetPasswordDto)
    {
        try
        {
            if (resetPasswordDto == null || string.IsNullOrWhiteSpace(resetPasswordDto.ResetToken) || 
                string.IsNullOrWhiteSpace(resetPasswordDto.NewPassword))
                return new Response<string>(HttpStatusCode.BadRequest, "Token ва рамзи нав ҳатмист");

            var tokenParts = resetPasswordDto.ResetToken.Split('_');
            if (tokenParts.Length != 2 || !int.TryParse(tokenParts[1], out var userId))
                return new Response<string>(HttpStatusCode.BadRequest, "Token нодуруст аст");

            var existingUser = await userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (existingUser == null)
                return new Response<string>(HttpStatusCode.NotFound, "Корбар ёфт нашуд");

            var expectedCode = $"VERIFIED_{resetPasswordDto.ResetToken}";
            if (existingUser.Code != expectedCode)
                return new Response<string>(HttpStatusCode.BadRequest, "Token нодуруст аст ё аллакай истифода шудааст");
            var timeElapsed = DateTime.UtcNow - existingUser.CodeDate;
            if (timeElapsed.TotalMinutes > 10)
                return new Response<string>(HttpStatusCode.BadRequest, "Мӯҳлати token гузаштааст. Лутфан, аз нав кӯшиш кунед");

            var passwordResetToken = await userManager.GeneratePasswordResetTokenAsync(existingUser);
            var resetResult = await userManager.ResetPasswordAsync(existingUser, passwordResetToken, resetPasswordDto.NewPassword);
            
            if (!resetResult.Succeeded)
                return new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(resetResult));

            existingUser.Code = null;
            existingUser.CodeDate = default;
            await context.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.OK, "Рамз бо муваффақият иваз карда шуд");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми барқарорсозии рамз: {ex.Message}");
        }
    }
}
