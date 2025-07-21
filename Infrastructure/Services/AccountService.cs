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
    string uploadPath) : IAccountService
{
    public async Task<Response<string>> Register(RegisterDto model)
    {
        try
        {
            var existingUser = await userManager.FindByNameAsync(model.UserName);
            if (existingUser != null)
                return new Response<string>(HttpStatusCode.BadRequest, "Username already exists");
            
            string profileImagePath = string.Empty;
            if (model.ProfileImage != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(
                    model.ProfileImage, uploadPath, "profiles", "profile");
                if (imageResult.StatusCode != 200)
                    return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);
                profileImagePath = imageResult.Data;
            }

            // Создание пользователя
            var userResult = await UserManagementHelper.CreateUserAsync(
                model,
                userManager,
                Roles.Student.ToString(),
                dto => dto.UserName,
                dto => dto.Email,
                dto => dto.FullName,
                dto => dto.Birthday,
                dto => dto.Gender,
                dto => dto.Address,
                dto => dto.CenterId,
                _ => profileImagePath,
                false); // Не использовать номер телефона как имя пользователя
            if (userResult.StatusCode != 200)
                return new Response<string>((HttpStatusCode)userResult.StatusCode, userResult.Message);

            var (_, password, username) = userResult.Data;

            // Отправка email
            if (!string.IsNullOrEmpty(model.Email))
            {
                await EmailHelper.SendLoginDetailsEmailAsync(
                    emailService,
                    model.Email,
                    username,
                    password,
                    "User",
                    "#5E60CE",
                    "#4EA8DE");
            }

            return new Response<string>("User registered successfully. Login credentials sent to user's email.");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<string>> Login(LoginDto login)
    {
        var user = await userManager.FindByNameAsync(login.Username);
        if (user == null)
            return new Response<string>(HttpStatusCode.NotFound, "Your password or username is invalid");

        var isPasswordValid = await userManager.CheckPasswordAsync(user, login.Password);
        if (!isPasswordValid)
            return new Response<string>(HttpStatusCode.BadRequest, "Your password or username is invalid");

        var token = await GenerateJwtToken(user);
        return new Response<string>(token) { Message = "Login successful" };
    }

    public async Task<Response<string>> AddRoleToUser(RoleDto userRole)
    {
        var user = await userManager.FindByIdAsync(userRole.UserId);
        if (user == null)
            return new Response<string>(HttpStatusCode.NotFound, "User not found");

        if (!await roleManager.RoleExistsAsync(userRole.RoleName))
            return new Response<string>(HttpStatusCode.BadRequest, "Role does not exist");

        var result = await userManager.AddToRoleAsync(user, userRole.RoleName);
        if (!result.Succeeded)
            return new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(result));

        return new Response<string>("Role added successfully");
    }

    public async Task<Response<List<string>>> GetUserRoles(int userId)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return new Response<List<string>>(HttpStatusCode.NotFound, "User not found");

            var roles = await userManager.GetRolesAsync(user);
            return new Response<List<string>>(roles.ToList()) { Message = "User roles retrieved successfully" };
        }
        catch (Exception ex)
        {
            return new Response<List<string>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<string>> RemoveRoleFromUser(RoleDto userRole)
    {
        var user = await userManager.FindByIdAsync(userRole.UserId);
        if (user == null)
            return new Response<string>(HttpStatusCode.NotFound, "User not found");

        var result = await userManager.RemoveFromRoleAsync(user, userRole.RoleName);
        if (!result.Succeeded)
            return new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(result));

        return new Response<string>("Role removed successfully");
    }
    private async Task<string> GenerateJwtToken(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "User cannot be null");
        }

        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]);
        var securityKey = new SymmetricSecurityKey(key);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString() )
        };

        if (!string.IsNullOrEmpty(user.UserName))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, user.UserName));
        }

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        }

        if (user.CenterId.HasValue)
        {
            claims.Add(new Claim("CenterId", user.CenterId.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(user.ProfileImagePath))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Picture, user.ProfileImagePath));
        }
        else
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Picture, "null"));
        }

        var roles = await userManager.GetRolesAsync(user);
        if (roles != null)
        {
            claims.AddRange(roles.Where(role => !string.IsNullOrEmpty(role)).Select(role => new Claim("role", role)));
        }

        // Only add CenterId claim if user is NOT SuperAdmin
        bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");
        if (!isSuperAdmin && user.CenterId.HasValue)
        {
            claims.Add(new Claim("CenterId", user.CenterId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(3),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return tokenString;
    }

    public async Task<Response<string>> ResetPassword(ResetPasswordDto resetPasswordDto)
    {
        try
        {
            if (resetPasswordDto == null)
                return new Response<string>(HttpStatusCode.BadRequest, "Invalid request data");

            var existingUser = await userManager.Users.FirstOrDefaultAsync(x => x.Email == resetPasswordDto.Email);
            if (existingUser == null)
                return new Response<string>(HttpStatusCode.NotFound, "User not found");

            if (resetPasswordDto.Code != existingUser.Code)
                return new Response<string>(HttpStatusCode.BadRequest, "Invalid code");

            var timeElapsed = DateTimeOffset.UtcNow - existingUser.CodeDate;
            if (timeElapsed.TotalMinutes > 3)
                return new Response<string>(HttpStatusCode.BadRequest, "Code expired");

            var resetToken = await userManager.GeneratePasswordResetTokenAsync(existingUser);
            var resetResult = await userManager.ResetPasswordAsync(existingUser, resetToken, resetPasswordDto.Password);
            if (!resetResult.Succeeded)
                return new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(resetResult));

            existingUser.Code = null;
            existingUser.CodeDate = default;
            await context.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.OK, "Password reset successfully");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<string>> ForgotPasswordCodeGenerator(ForgotPasswordDto forgotPasswordDto)
    {
        try
        {
            if (forgotPasswordDto == null)
                return new Response<string>(HttpStatusCode.BadRequest, "Invalid request data");

            var existingUser = await context.Users.FirstOrDefaultAsync(x => x.Email == forgotPasswordDto.Email);
            if (existingUser == null)
                return new Response<string>(HttpStatusCode.NotFound, "User not found");

            var code = new Random().Next(1000, 9999).ToString();
            existingUser.Code = code;
            existingUser.CodeDate = DateTime.UtcNow;

            var res = await context.SaveChangesAsync();
            if (res <= 0)
                return new Response<string>(HttpStatusCode.BadRequest, "Could not generate reset code");

            await EmailHelper.SendResetPasswordCodeEmailAsync(emailService, forgotPasswordDto.Email, code);

            return new Response<string>(HttpStatusCode.OK, "Reset code sent successfully");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<string>> ChangePassword(ChangePasswordDto passwordDto, int userId)
    {
        try
        {
            if (passwordDto == null)
                return new Response<string>(HttpStatusCode.BadRequest, "Invalid password data");

            var existingUser = await userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (existingUser == null)
                return new Response<string>(HttpStatusCode.NotFound, "User not found");

            var changePassResult = await userManager.ChangePasswordAsync(existingUser, passwordDto.OldPassword, passwordDto.Password);
            if (!changePassResult.Succeeded)
                return new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(changePassResult));

            return new Response<string>(HttpStatusCode.OK, "Password changed successfully");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}