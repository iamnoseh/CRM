using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Net;
using Domain.DTOs.Account;
using Domain.DTOs.EmailDTOs;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Services.EmailService;
using Infrastructure.Services.HashService;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MimeKit.Text;

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
    private readonly string[] _allowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif"];
    private const long MaxImageSize = 10 * 1024 * 1024; // 10MB

    #region Register
    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }

    public async Task<Response<string>> Register(RegisterDto model)
    {
        var existingUser = await userManager.FindByNameAsync(model.PhoneNumber);
        if (existingUser != null)
            return new Response<string>(HttpStatusCode.BadRequest, "Phone Number already exists");
        string profileImagePath = string.Empty;
        if (model.ProfileImage != null && model.ProfileImage.Length > 0)
        {
            var fileExtension = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
            if (!_allowedImageExtensions.Contains(fileExtension))
                return new Response<string>(HttpStatusCode.BadRequest,
                    "Invalid profile image format. Allowed formats: .jpg, .jpeg, .png, .gif");

            if (model.ProfileImage.Length > MaxImageSize)
                return new Response<string>(HttpStatusCode.BadRequest, "Profile image size must be less than 10MB");

            var profilesFolder = Path.Combine(uploadPath, "uploads", "profiles");
            if (!Directory.Exists(profilesFolder))
                Directory.CreateDirectory(profilesFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(profilesFolder, uniqueFileName);

            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.ProfileImage.CopyToAsync(fileStream);
            }

            profileImagePath = $"/uploads/profiles/{uniqueFileName}";
        }

        
        string password = GenerateRandomPassword();

        var newUser = new User
        {
            FullName = model.FullName,
            UserName = model.UserName,
            Email = model.Email,
            Birthday = model.Birthday,
            Age = CalculateAge(model.Birthday),
            ProfileImagePath = profileImagePath,
            Address = model.Address,
            PhoneNumber = model.PhoneNumber,
        };

        var result = await userManager.CreateAsync(newUser, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return new Response<string>(HttpStatusCode.BadRequest, errors);
        }

        // Send login credentials to user's email
        await SendLoginCredentialsEmail(newUser.Email, newUser.UserName, password);
        
        return new Response<string>("User registered successfully. Login credentials sent to user's email.");
    }

    private string GenerateRandomPassword()
    {
        // Generate a random password with at least one uppercase, one lowercase, one digit, and one special character
        const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
        const string digitChars = "0123456789";
        const string specialChars = "&-.";
        
        var random = new Random();
        var password = new StringBuilder();

        // Ensure at least one uppercase letter
        password.Append(uppercaseChars[random.Next(uppercaseChars.Length)]);
        
        // Ensure at least one lowercase letter
        password.Append(lowercaseChars[random.Next(lowercaseChars.Length)]);
        
        // Add more digits (at least 4 digits)
        for (int i = 0; i < 4; i++)
        {
            password.Append(digitChars[random.Next(digitChars.Length)]);
        }
        
        // Add one special character
        password.Append(specialChars[random.Next(specialChars.Length)]);
        
        // Fill the rest with lowercase letters to make it 8 characters long
        while (password.Length < 8)
        {
            password.Append(lowercaseChars[random.Next(lowercaseChars.Length)]);
        }

        // Shuffle the password characters
        return new string(password.ToString().OrderBy(c => random.Next()).ToArray());
    }

    private async Task SendLoginCredentialsEmail(string email, string username, string password)
    {
        string emailSubject = "Your CRM Account Login Credentials";
        string emailContent = $@"
            <h1>Welcome to our CRM System</h1>
            <p>Your account has been created successfully. Below are your login credentials:</p>
            <p><strong>Username:</strong> {username}</p>
            <p><strong>Password:</strong> {password}</p>
            <p>Please keep this information secure and change your password after your first login.</p>
            <p>Thank you for using our system!</p>
        ";

        await emailService.SendEmail(
            new EmailMessageDto(new[] { email }, emailSubject, emailContent),
            TextFormat.Html
        );
    }

    #endregion


    #region Login

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

    #endregion

    #region AddRoleToUser

    public async Task<Response<string>> AddRoleToUser(RoleDto userRole)
    {
        var user = await userManager.FindByIdAsync(userRole.UserId);
        if (user == null)
            return new Response<string>(HttpStatusCode.NotFound, "User not found");

        if (!await roleManager.RoleExistsAsync(userRole.RoleName))
            return new Response<string>(HttpStatusCode.BadRequest, "Role does not exist");

        var result = await userManager.AddToRoleAsync(user, userRole.RoleName);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return new Response<string>(HttpStatusCode.BadRequest, errors);
        }

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

    #endregion

    #region RemoveRoleFromUser

    public async Task<Response<string>> RemoveRoleFromUser(RoleDto userRole)
    {
        var user = await userManager.FindByIdAsync(userRole.UserId);
        if (user == null)
            return new Response<string>(HttpStatusCode.NotFound, "User not found");

        var result = await userManager.RemoveFromRoleAsync(user, userRole.RoleName);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return new Response<string>(HttpStatusCode.BadRequest, errors);
        }

        return new Response<string>("Role removed successfully");
    }

    #endregion

    #region GenerateJwtToken

    private async Task<string> GenerateJwtToken(User user)
    {
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]);
        var securityKey = new SymmetricSecurityKey(key);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Name, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
            new Claim("CenterId",user.CenterId.ToString()!)
        };

        var roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim("role", role)));

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

    #endregion

    #region ResetPassword
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

            // Генерация токена для сброса пароля
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(existingUser);

            // Сброс пароля
            var resetResult = await userManager.ResetPasswordAsync(existingUser, resetToken, resetPasswordDto.Password);
            if (!resetResult.Succeeded)
            {
                var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                return new Response<string>(HttpStatusCode.BadRequest, errors);
            }

            
            existingUser.Code = null;
            existingUser.CodeDate = default;
            await context.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.OK, "Password reset successfully");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.BadRequest, ex.Message);
        }
    }

    #endregion

    #region ForgotPasswordCodeGenerator
    public async Task<Response<string>> ForgotPasswordCodeGenerator(ForgotPasswordDto forgotPasswordDto)
    {
        try
        {
            if (forgotPasswordDto == null)
            {
                return new Response<string>(HttpStatusCode.BadRequest, "Invalid request data");
            }

            var existingUser = await context.Users.FirstOrDefaultAsync(x => x.Email == forgotPasswordDto.Email);
            if (existingUser == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, "User not found");
            }

            var code = new Random().Next(1000, 9999);
            var resetToken = code.ToString();
            existingUser.Code = resetToken;
            existingUser.CodeDate = DateTime.UtcNow;

            var res = await context.SaveChangesAsync();
            if (res <= 0)
            {
                return new Response<string>(HttpStatusCode.BadRequest, "Could not generate reset code");
            }
            
            string emailContent = $"<h1>Your password reset code is:</h1><p>{resetToken}</p>";
            await emailService.SendEmail(
                new EmailMessageDto(new[] { forgotPasswordDto.Email }, "Reset Password Code", emailContent),
                TextFormat.Html
            );

            return new Response<string>(HttpStatusCode.OK, "Reset code sent successfully");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.BadRequest, ex.Message);
        }
    }
    #endregion
    
    #region ChangePassword
    public async Task<Response<string>> ChangePassword(ChangePasswordDto passwordDto, int userId)
    {
        try
        {
            if (passwordDto == null)
                return new Response<string>(HttpStatusCode.BadRequest, "Invalid password data");

            var existingUser = await userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (existingUser == null)
                return new Response<string>(HttpStatusCode.BadRequest, "User not found");

            // Истифодаи UserManager барои иваз кардани парол
            var changePassResult = await userManager.ChangePasswordAsync(existingUser, passwordDto.OldPassword, passwordDto.Password);
            if (!changePassResult.Succeeded)
            {
                var errors = string.Join("; ", changePassResult.Errors.Select(e => e.Description));
                return new Response<string>(HttpStatusCode.BadRequest, errors);
            }

            return new Response<string>(HttpStatusCode.OK, "Password changed successfully");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.BadRequest, ex.Message);
        }
    }
    #endregion


    
}