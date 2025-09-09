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
    IOsonSmsService osonSmsService,
    string uploadPath) : IAccountService
{
    public async Task<Response<string>> Register(RegisterDto model)
    {
        try
        {
            var existingUser = await userManager.FindByNameAsync(model.UserName);
            if (existingUser != null)
                return new Response<string>(HttpStatusCode.BadRequest, "Чунин номи корбар аллакай вуҷуд дорад");
            
            string profileImagePath = string.Empty;
            if (model.ProfileImage != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(
                    model.ProfileImage, 
                    uploadPath,
                    "profiles",
                    "profile",
                    true);

                if (imageResult.StatusCode != (int)HttpStatusCode.OK)
                    return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);

                profileImagePath = imageResult.Data;
            }

            var userResult = await UserManagementHelper.CreateUserAsync(
                model,
                userManager,
                Roles.User.ToString(),
                dto => dto.UserName,
                dto => dto.Email,
                dto => dto.FullName,
                dto => dto.Birthday,
                dto => dto.Gender,
                dto => dto.Address,
                dto => dto.CenterId,
                _ => profileImagePath,
                false); 

            if (userResult.StatusCode != (int)HttpStatusCode.OK)
                return new Response<string>((HttpStatusCode)userResult.StatusCode, userResult.Message);

            var (user, password, username) = userResult.Data;

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
            
            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                var loginUrl = configuration["AppSettings:LoginUrl"];
                var smsMessage = $"Салом, {user.FullName}!\nНоми корбар: {username},\nПарол: {password}.\nЛутфан, барои ворид шудан ба система ба ин суроға ташриф оред: {loginUrl}\nKavsar Academy";
                await osonSmsService.SendSmsAsync(user.PhoneNumber, smsMessage);
            }

            return new Response<string>(HttpStatusCode.Created, "Корбар бо муваффақият сохта шуд. Маълумоти воридшавӣ ба почтаи электронӣ ва/ё SMS фиристода шуд.");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми бақайдгирии корбар: {ex.Message}");
        }
    }

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

        // Determine principal identifier to embed into JWT (Student.Id / Mentor.Id / User.Id)
        var roles = await userManager.GetRolesAsync(user);
        int principalId = user.Id;
        string principalType = "User";

        if (roles != null && roles.Contains("Student"))
        {
            var student = await context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student != null)
            {
                principalId = student.Id;
                principalType = "Student";
            }
        }
        else if (roles != null && roles.Contains("Mentor"))
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

        if (roles != null)
        {
            claims.AddRange(roles.Where(role => !string.IsNullOrEmpty(role)).Select(role => new Claim("role", role)));
        }

        bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");
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

    public async Task<Response<string>> ResetPassword(ResetPasswordDto resetPasswordDto)
    {
        try
        {
            if (resetPasswordDto == null)
                return new Response<string>(HttpStatusCode.BadRequest, "Маълумоти дархост нодуруст аст");

            var existingUser = await userManager.FindByNameAsync(resetPasswordDto.Username);
            if (existingUser == null)
                return new Response<string>(HttpStatusCode.NotFound, "Корбар ёфт нашуд");

            if (resetPasswordDto.Code != existingUser.Code)
                return new Response<string>(HttpStatusCode.BadRequest, "Рамзи тасдиқ нодуруст аст");

            var timeElapsed = DateTimeOffset.UtcNow - existingUser.CodeDate;
            if (timeElapsed.TotalMinutes > 3)
                return new Response<string>(HttpStatusCode.BadRequest, "Мӯҳлати рамзи тасдиқ гузаштааст");

            var resetToken = await userManager.GeneratePasswordResetTokenAsync(existingUser);
            var resetResult = await userManager.ResetPasswordAsync(existingUser, resetToken, resetPasswordDto.Password);
            if (!resetResult.Succeeded)
                return new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(resetResult));

            existingUser.Code = null;
            existingUser.CodeDate = default;
            await context.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.OK, "Рамз бо муваффақият иваз карда шуд");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми ивазкунии рамз: {ex.Message}");
        }
    }

    public async Task<Response<string>> ForgotPasswordCodeGenerator(ForgotPasswordDto forgotPasswordDto)
    {
        try
        {
            if (forgotPasswordDto == null)
                return new Response<string>(HttpStatusCode.BadRequest, "Маълумоти дархост нодуруст аст");

            var existingUser = await userManager.FindByNameAsync(forgotPasswordDto.Username);
            if (existingUser == null)
                return new Response<string>(HttpStatusCode.NotFound, "Корбар ёфт нашуд");

            var code = new Random().Next(1000, 9999).ToString();
            existingUser.Code = code;
            existingUser.CodeDate = DateTime.UtcNow;

            var res = await context.SaveChangesAsync();
            if (res <= 0)
                return new Response<string>(HttpStatusCode.BadRequest, "Хатогӣ ҳангоми сохтани рамзи тасдиқ");

            if (!string.IsNullOrWhiteSpace(existingUser.Email))
            {
                await EmailHelper.SendResetPasswordCodeEmailAsync(emailService, existingUser.Email, code);
            }

            return new Response<string>(HttpStatusCode.OK, "Рамзи тасдиқ бо муваффақият сохта шуд  фиристода шуд");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогӣ ҳангоми фиристодани рамзи тасдиқ: {ex.Message}");
        }
    }

    public async Task<Response<string>> ChangePassword(ChangePasswordDto passwordDto, int userId)
    {
        try
        {
            if (passwordDto == null)
                return new Response<string>(HttpStatusCode.BadRequest, "Маълумоти рамз нодуруст аст");

            var existingUser = await userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
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
}