using Domain.DTOs.Student;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Infrastructure.Services.EmailService;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Security.Cryptography;
using Serilog;

namespace Infrastructure.Features.Students.Commands.CreateStudent;

public class CreateStudentHandler(
    DataContext context,
    IHttpContextAccessor httpContextAccessor,
    UserManager<User> userManager,
    IEmailService emailService,
    IOsonSmsService osonSmsService,
    IConfiguration configuration) : IRequestHandler<CreateStudentCommand, Response<string>>
{
    public async Task<Response<string>> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var createStudentDto = request.Dto;
            var uploadPath = configuration["UploadPath"] ?? "wwwroot";

            Log.Information("Создание студента: {Email}, {FullName}", createStudentDto.Email, createStudentDto.FullName);

            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
            {
                Log.Warning("CenterId не найден в токене");
                return new Response<string>(HttpStatusCode.BadRequest, "CenterId не найден в токене");
            }

            string profileImagePath = string.Empty;
            if (createStudentDto.ProfilePhoto != null)
            {
                var imageResult = await FileUploadHelper.UploadFileAsync(
                    createStudentDto.ProfilePhoto, uploadPath, "profiles", "profile");
                if (imageResult.StatusCode != 200)
                {
                    Log.Warning("Ошибка загрузки изображения профиля: {Message}", imageResult.Message);
                    return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);
                }
                profileImagePath = imageResult.Data;
            }

            string documentPath = string.Empty;
            if (createStudentDto.DocumentFile != null)
            {
                var docResult = await FileUploadHelper.UploadFileAsync(
                    createStudentDto.DocumentFile, uploadPath, "student", "document");
                if (docResult.StatusCode != 200)
                {
                    Log.Warning("Ошибка загрузки документа: {Message}", docResult.Message);
                    return new Response<string>((HttpStatusCode)docResult.StatusCode, docResult.Message);
                }
                documentPath = docResult.Data;
            }


            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var userResult = await UserManagementHelper.CreateUserAsync(
                    createStudentDto,
                    userManager,
                    Roles.Student,
                    dto => dto.PhoneNumber,
                    dto => dto.Email,
                    dto => dto.FullName,
                    dto => dto.Birthday,
                    dto => dto.Gender,
                    dto => dto.Address,
                    dto => centerId.Value,
                    _ => profileImagePath);

                if (userResult.StatusCode != 200)
                {
                    Log.Warning("Ошибка создания пользователя: {Message}", userResult.Message);
                    return new Response<string>((HttpStatusCode)userResult.StatusCode, userResult.Message);
                }

                var (user, password, username) = userResult.Data;

                var student = new Student
                {
                    FullName = createStudentDto.FullName,
                    Email = createStudentDto.Email,
                    Address = createStudentDto.Address,
                    PhoneNumber = createStudentDto.PhoneNumber,
                    Birthday = createStudentDto.Birthday,
                    Age = DateUtils.CalculateAge(createStudentDto.Birthday),
                    Gender = createStudentDto.Gender,
                    CenterId = centerId.Value,
                    UserId = user.Id,
                    ProfileImage = profileImagePath,
                    Document = documentPath,
                    ActiveStatus = ActiveStatus.Active,
                    PaymentStatus = PaymentStatus.Pending
                };

                await context.Students.AddAsync(student, cancellationToken);

                
                var walletCode = await GenerateUniqueWalletCodeAsync(cancellationToken);
                var account = new StudentAccount
                {
                    StudentId = student.Id,
                    AccountCode = walletCode,
                    Balance = 0,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                await context.StudentAccounts.AddAsync(account, cancellationToken);

                // ✅ Single SaveChanges
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                Log.Information("Студент успешно создан: {StudentId}, Кошелек: {WalletCode}", student.Id, walletCode);

                // Send notifications after successful commit
                if (!string.IsNullOrEmpty(createStudentDto.Email))
                {
                    await EmailHelper.SendLoginDetailsEmailAsync(
                        emailService,
                        createStudentDto.Email,
                        username,
                        password,
                        "Student",
                        "#5E60CE",
                        "#4EA8DE");
                }

                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    var loginUrl = configuration["AppSettings:LoginUrl"];
                    var smsMessage = $"Салом, {user.FullName}!\nUsername: {username},\nPassword: {password}\nЛутфан, барои ворид шудан ба система ба ин суроға ташриф оред: {loginUrl}\nKavsar Academy";
                    await osonSmsService.SendSmsAsync(user.PhoneNumber, smsMessage);

                    var walletSms = $"Салом, {student.FullName}!\nКоди ҳамёни шумо: {walletCode}.\nИн кодро ҳангоми пур кардани ҳисоб ҳатман ба админ пешниҳод кунед. Лутфан рамзро нигоҳ доред ва гум накунед.";
                    await osonSmsService.SendSmsAsync(student.PhoneNumber, walletSms);
                }

                return new Response<string>(HttpStatusCode.Created, "Донишҷӯ бо муваффақият сохта шуд");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                Log.Error(ex, "Откат транзакции при создании студента");
                throw;
            }
        }
        catch (DbUpdateException ex)
        {
            Log.Error(ex, "Ошибка базы данных при создании студента: {Email}", request.Dto.Email);
            return new Response<string>(HttpStatusCode.InternalServerError, "Хатогӣ дар бонки иттилоот");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка создания студента: {Email}", request.Dto.Email);
            return new Response<string>(HttpStatusCode.InternalServerError, "Хатогии нобаррасӣ");
        }
    }

    // ✅ Improved wallet code generation
    private async Task<string> GenerateUniqueWalletCodeAsync(CancellationToken cancellationToken, int maxAttempts = 10)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var code = GenerateSecureCode();
            var exists = await context.StudentAccounts.AnyAsync(a => a.AccountCode == code, cancellationToken);
            if (!exists)
            {
                Log.Debug("Код кошелька сгенерирован с попытки {Attempt}", attempt + 1);
                return code;
            }

            Log.Warning("Коллизия кода кошелька на попытке {Attempt}", attempt + 1);
        }

        Log.Error("Не удалось сгенерировать уникальный код кошелька после {MaxAttempts} попыток", maxAttempts);
        throw new InvalidOperationException("Наметавонад коди фарди ҳамён созад");
    }

    private static string GenerateSecureCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[3];
        rng.GetBytes(bytes);
        var number = BitConverter.ToUInt32([bytes[0], bytes[1], bytes[2], 0]) % 1000000;
        return number.ToString("D6");
    }
}
