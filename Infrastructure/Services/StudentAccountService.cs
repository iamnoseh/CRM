using System.Net;
using Domain.DTOs.Finance;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Services;

    public class StudentAccountService(
        DataContext db,
        IDiscountService discountService,
        IMessageSenderService messageSenderService,
        IHttpContextAccessor httpContextAccessor
    ) : IStudentAccountService
{
    public async Task<Response<GetStudentAccountDto>> GetByStudentIdAsync(int studentId)
    {
        var account = await db.StudentAccounts.FirstOrDefaultAsync(a => a.StudentId == studentId && !a.IsDeleted);
        if (account == null)
        {
            // auto-create
            account = new StudentAccount
            {
                StudentId = studentId,
                AccountCode = await GenerateUniqueCodeAsync(),
                Balance = 0,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.StudentAccounts.Add(account);
            await db.SaveChangesAsync();
        }
        return new Response<GetStudentAccountDto>(Map(account));
    }

    public async Task<Response<List<GetAccountLogDto>>> GetLastLogsAsync(int studentId, int limit = 10)
    {
        try
        {
            var account = await db.StudentAccounts.FirstOrDefaultAsync(a => a.StudentId == studentId && !a.IsDeleted);
            if (account == null)
            {
                return new Response<List<GetAccountLogDto>>(HttpStatusCode.OK, "Ҳисоб ҳоло вуҷуд надорад")
                {
                    Data = new List<GetAccountLogDto>()
                };
            }

            if (limit <= 0) limit = 10;

            var logs = await db.AccountLogs
                .Where(l => l.AccountId == account.Id && !l.IsDeleted)
                .OrderByDescending(l => l.CreatedAt)
                .Take(limit)
                .Select(l => new GetAccountLogDto
                {
                    Id = l.Id,
                    AccountId = l.AccountId,
                    StudentId = account.StudentId,
                    StudentFullName = db.Students.Where(s => s.Id == account.StudentId).Select(s => s.FullName).FirstOrDefault()!,
                    AccountCode = account.AccountCode,
                    Amount = l.Amount,
                    Type = TranslateType(l.Type),
                    Note = l.Note,
                    CreatedAt = l.CreatedAt,
                    PerformedByUserId = l.PerformedByUserId,
                    PerformedByName = l.PerformedByName
                })
                .ToListAsync();

            return new Response<List<GetAccountLogDto>>(logs)
            {
                Message = "Рӯйхати охирини амалиётҳо"
            };
        }
        catch (Exception ex)
        {
            var fullName = await db.Students.Where(s => s.Id == studentId).Select(s => s.FullName).FirstOrDefaultAsync();
            Log.Error(ex, "Гирифтани амалиётҳои охирин ноком шуд барои донишҷӯ: {FullName}", fullName ?? "номаълум");
            return new Response<List<GetAccountLogDto>>(HttpStatusCode.InternalServerError, "Хатои дохилӣ ҳангоми боркунии амалиётҳо");
        }
    }

    public async Task<Response<GetStudentAccountDto>> TopUpAsync(TopUpDto dto)
    {
        try
        {
            var account = await db.StudentAccounts.FirstOrDefaultAsync(a => a.AccountCode == dto.AccountCode && a.IsActive && !a.IsDeleted);
            if (account == null)
                return new Response<GetStudentAccountDto>(HttpStatusCode.NotFound, "Ҳисоб ёфт нашуд ё ғайрифаъол аст");

            if (dto.Amount <= 0)
                return new Response<GetStudentAccountDto>(HttpStatusCode.BadRequest, "Маблағ бояд > 0 бошад");

            account.Balance += dto.Amount;
            account.UpdatedAt = DateTimeOffset.UtcNow;

            var (userId, userName) = GetCurrentUser();

            var log = new AccountLog
            {
                AccountId = account.Id,
                Amount = dto.Amount,
                Type = "TopUp",
                Note = string.IsNullOrWhiteSpace(dto.Notes) ? dto.Method : $"{dto.Method}: {dto.Notes}",
                PerformedByUserId = userId,
                PerformedByName = userName,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            db.AccountLogs.Add(log);
            await db.SaveChangesAsync();
            Log.Information("TopUp: AccountId={AccountId} Amount={Amount}", account.Id, dto.Amount);
            return new Response<GetStudentAccountDto>(Map(account)) { Message = "Баланс муваффақона пур шуд" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "TopUp ноком шуд барои AccountCode={AccountCode}", dto.AccountCode);
            return new Response<GetStudentAccountDto>(HttpStatusCode.InternalServerError, "Хатои дохилӣ");
        }
    }

    public async Task<Response<int>> RunMonthlyChargeAsync(int month, int year)
    {
        try
        {
            var date = new DateTime(year, month, 1);

            var studentGroups = await db.StudentGroups
                .Include(sg => sg.Student)
                .Include(sg => sg.Group).ThenInclude(g => g.Course)
                .Where(sg => !sg.IsDeleted && sg.IsActive)
                .ToListAsync();

            var accountsByStudent = await db.StudentAccounts.Where(a => a.IsActive && !a.IsDeleted)
                .ToDictionaryAsync(a => a.StudentId, a => a);

            var successCount = 0;

            foreach (var sg in studentGroups)
            {
                if (sg.Student == null || sg.Group == null || sg.Group.Course == null)
                    continue;

                if (!accountsByStudent.TryGetValue(sg.StudentId, out var account))
                {
                    continue; // no account
                }

                var preview = await discountService.PreviewAsync(sg.StudentId, sg.GroupId, month, year);
                if (preview.StatusCode != (int)HttpStatusCode.OK || preview.Data == null)
                {
                    continue;
                }

                var amountToCharge = preview.Data.PayableAmount;
                if (amountToCharge <= 0)
                    continue;

                if (account.Balance >= amountToCharge)
                {
                    // debit
                    account.Balance -= amountToCharge;
                    account.UpdatedAt = DateTimeOffset.UtcNow;

                    db.AccountLogs.Add(new AccountLog
                    {
                        AccountId = account.Id,
                        Amount = -amountToCharge,
                        Type = "MonthlyCharge",
                        Note = $"{month:00}.{year} GroupId={sg.GroupId}",
                        PerformedByUserId = null,
                        PerformedByName = "Система",
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    });

                    // also create Payment for finance reports
                    var payment = new Payment
                    {
                        StudentId = sg.StudentId,
                        GroupId = sg.GroupId,
                        OriginalAmount = preview.Data.OriginalAmount,
                        DiscountAmount = preview.Data.DiscountAmount,
                        Amount = amountToCharge,
                        PaymentMethod = PaymentMethod.Other,
                        TransactionId = null,
                        Description = "Пардохт аз ҳисоби донишҷӯ",
                        Status = PaymentStatus.Completed,
                        PaymentDate = DateTime.UtcNow,
                        CenterId = sg.Group.Course.CenterId,
                        Month = month,
                        Year = year,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    db.Payments.Add(payment);

                    successCount++;
                }
                else
                {
                    // notify low balance
                    await NotifyInsufficientAsync(sg.StudentId, account, amountToCharge, date);
                }
            }

            await db.SaveChangesAsync();
            return new Response<int>(successCount) { Message = $"Дебет муваффақ шуд барои {successCount} донишҷӯ" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Monthly charge ноком шуд {Month}.{Year}", month, year);
            return new Response<int>(HttpStatusCode.InternalServerError, "Хатои дохилӣ дар monthly charge");
        }
    }

    private async Task<string> GenerateUniqueCodeAsync()
    {
        var rnd = new Random();
        while (true)
        {
            var code = rnd.Next(0, 999999).ToString("D6");
            var exists = await db.StudentAccounts.AnyAsync(a => a.AccountCode == code);
            if (!exists) return code;
        }
    }

    private async Task NotifyInsufficientAsync(int studentId, StudentAccount account, decimal required, DateTime dueDate)
    {
        try
        {
            var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student == null) return;

            var missing = required - account.Balance;
            var sms = $"Баланс нокифоя. Лозим: {required:0.##}, камбуд: {missing:0.##}. Код: {account.AccountCode}";
            if (!string.IsNullOrWhiteSpace(student.PhoneNumber))
            {
                await messageSenderService.SendSmsToNumberAsync(student.PhoneNumber, sms);
            }

            if (!string.IsNullOrWhiteSpace(student.Email))
            {
                await messageSenderService.SendEmailToAddressAsync(new Domain.DTOs.MessageSender.SendEmailToAddressDto
                {
                    EmailAddress = student.Email,
                    Subject = "Норасоии баланс",
                    MessageContent = $"<p>Салом, {student.FullName}.</p><p>Барои пардохти моҳонаи {dueDate:MM.yyyy} баланс нокифоя аст.</p><p>Маблағи лозим: {required:0.##}. Код: <b>{account.AccountCode}</b>.</p>"
                });
            }
        }
        catch (Exception ex)
        {
            var fullName = await db.Students.Where(s => s.Id == studentId).Select(s => s.FullName).FirstOrDefaultAsync();
            Log.Warning(ex, "Огоҳӣ ноком шуд барои донишҷӯ: {FullName}", fullName ?? "номаълум");
        }
    }

    private static GetStudentAccountDto Map(StudentAccount a) => new()
    {
        Id = a.Id,
        StudentId = a.StudentId,
        AccountCode = a.AccountCode,
        Balance = a.Balance,
        IsActive = a.IsActive
    };

    private (int? userId, string? userName) GetCurrentUser()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user == null) return (null, null);
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        int? id = null;
        if (int.TryParse(idStr, out var parsed)) id = parsed;
        var name = user.Identity?.Name;
        if (string.IsNullOrWhiteSpace(name) && id.HasValue)
        {
            var u = db.Users.FirstOrDefault(x => x.Id == id.Value);
            name = u?.FullName ?? u?.UserName;
        }
        return (id, name);
    }

    private static string TranslateType(string type)
    {
        return type switch
        {
            "TopUp" => "Пуркунӣ",
            "MonthlyCharge" => "Пардохти моҳона",
            "Refund" => "Баргардонидан",
            "Adjustment" => "Ислоҳ",
            _ => type
        };
    }
}


