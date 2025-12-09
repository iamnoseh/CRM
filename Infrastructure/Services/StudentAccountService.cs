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
using Domain.Filters;
using Microsoft.EntityFrameworkCore.Infrastructure;

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

    public async Task<PaginationResponse<List<AccountListItemDto>>> GetAccountsAsync(string? search, int pageNumber, int pageSize)
    {
        var query = db.StudentAccounts.AsNoTracking()
            .Include(a => a.Student)
            .Where(a => !a.IsDeleted && a.Student != null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => EF.Functions.ILike(a.Student!.FullName, $"%{search.Trim()}%"));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.Student!.FullName)
            .Skip((pageNumber <= 1 ? 0 : (pageNumber - 1) * (pageSize <= 0 ? 10 : pageSize)))
            .Take(pageSize <= 0 ? 10 : pageSize)
            .Select(a => new AccountListItemDto
            {
                StudentId = a.StudentId,
                FullName = a.Student!.FullName,
                AccountCode = a.AccountCode
            })
            .ToListAsync();

        return new PaginationResponse<List<AccountListItemDto>>(items, total, pageNumber <= 0 ? 1 : pageNumber, pageSize <= 0 ? 10 : pageSize);
    }

    public async Task<Response<MyWalletDto>> GetMyWalletAsync(int limit = 10)
    {
        try
        {
            var user = httpContextAccessor.HttpContext?.User;
            var idStr = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out var userId))
                return new Response<MyWalletDto>(HttpStatusCode.Unauthorized, "Корбар муайян нашуд");

            var student = await db.Students.FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);
            if (student == null)
                return new Response<MyWalletDto>(HttpStatusCode.NotFound, "Профили донишҷӯ ёфт нашуд");

            var accountResp = await GetByStudentIdAsync(student.Id);
            if (accountResp.Data == null)
                return new Response<MyWalletDto>((HttpStatusCode)accountResp.StatusCode, accountResp.Message ?? "Ҳисоб ёфт нашуд");

            var logsResp = await GetLastLogsAsync(student.Id, limit <= 0 ? 10 : limit);
            var dto = new MyWalletDto
            {
                Account = accountResp.Data,
                LastLogs = logsResp.Data ?? new List<GetAccountLogDto>()
            };
            return new Response<MyWalletDto>(dto);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "GetMyWalletAsync failure");
            return new Response<MyWalletDto>(HttpStatusCode.InternalServerError, "Хатои дохилӣ");
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

            try
            {
                var student = await db.Students.FirstOrDefaultAsync(s => s.Id == account.StudentId && !s.IsDeleted);
                if (student != null && !string.IsNullOrWhiteSpace(student.PhoneNumber))
                {
                    var smsText = $"Салом, {student.FullName}! Ҳисоби шумо ба маблағи {dto.Amount:0.##} сомонӣ пур шуд. Тавозуни ҷорӣ: {account.Balance:0.##} сомонӣ. Ташаккур барои ҳамкорӣ бо мо.";
                    await messageSenderService.SendSmsToNumberAsync(student.PhoneNumber, smsText);
                }
                await RetryPendingForStudentAsync(account.StudentId);
            }
            catch { /* ignore sms errors */ }

            Log.Information("TopUp: AccountId={AccountId} Amount={Amount}", account.Id, dto.Amount);
            return new Response<GetStudentAccountDto>(Map(account)) { Message = "Баланс муваффақона пур шуд" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "TopUp ноком шуд барои AccountCode={AccountCode}", dto.AccountCode);
            return new Response<GetStudentAccountDto>(HttpStatusCode.InternalServerError, "Хатои дохилӣ");
        }
    }

    public async Task<Response<GetStudentAccountDto>> WithdrawAsync(WithdrawDto dto)
    {
        try
        {

            var account = await db.StudentAccounts.FirstOrDefaultAsync(a => a.AccountCode == dto.AccountCode && a.IsActive && !a.IsDeleted);
            if (account == null)
                return new Response<GetStudentAccountDto>(HttpStatusCode.NotFound, "Ҳисоб ёфт нашуд ё ғайрифаъол аст");


            if (dto.Amount <= 0)
                return new Response<GetStudentAccountDto>(HttpStatusCode.BadRequest, "Маблағ бояд > 0 бошад");

            if (account.Balance < dto.Amount)
                return new Response<GetStudentAccountDto>(HttpStatusCode.BadRequest, $"Маблағ нокифоя аст. Баланси ҷорӣ: {account.Balance:0.##} сомонӣ");


            account.Balance -= dto.Amount;
            account.UpdatedAt = DateTimeOffset.UtcNow;

            var (userId, userName) = GetCurrentUser();

            var log = new AccountLog
            {
                AccountId = account.Id,
                Amount = -dto.Amount,
                Type = "Withdraw",
                Note = string.IsNullOrWhiteSpace(dto.Reason) ? "Withdraw" : dto.Reason,
                PerformedByUserId = userId,
                PerformedByName = userName,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            db.AccountLogs.Add(log);
            await db.SaveChangesAsync();


            Log.Information("Withdraw: AccountId={AccountId} Amount={Amount} AccountCode={AccountCode}", account.Id, dto.Amount, dto.AccountCode);
            return new Response<GetStudentAccountDto>(Map(account)) { Message = "Маблағ муваффақона кам шуд" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Withdraw ноком шуд барои AccountCode={AccountCode}", dto.AccountCode);
            return new Response<GetStudentAccountDto>(HttpStatusCode.InternalServerError, "Хатои дохилӣ");
        }
    }

    public async Task<Response<int>> RunMonthlyChargeAsync(int month, int year)
    {
        try
        {
            var date = new DateTime(year, month, 1);

            var boundaryNow = DateTimeOffset.UtcNow;
            var studentGroups = await db.StudentGroups
                .Include(sg => sg.Student)
                .Include(sg => sg.Group).ThenInclude(g => g.Course)
                .Where(sg => !sg.IsDeleted && sg.IsActive &&
                             sg.Group != null &&
                             !sg.Group.IsDeleted &&
                             sg.Group.Status == ActiveStatus.Active &&
                             sg.Group.EndDate > boundaryNow)
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
                    continue;
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
                    account.Balance -= amountToCharge;
                    account.UpdatedAt = DateTimeOffset.UtcNow;

                    var groupName1 = sg.Group.Name;
                    db.AccountLogs.Add(new AccountLog
                    {
                        AccountId = account.Id,
                        Amount = -amountToCharge,
                        Type = "MonthlyCharge",
                        Note = $"{month:00}.{year} - Пардохт барои гурӯҳ {groupName1}",
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
                        Description = $"Пардохт барои гурӯҳ {groupName1} (аз ҳисоби донишҷӯ)",
                        Status = PaymentStatus.Completed,
                        PaymentDate = DateTime.UtcNow,
                        CenterId = sg.Group.Course.CenterId,
                        Month = month,
                        Year = year,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    db.Payments.Add(payment);

                    var studentToUpdate = await db.Students.FirstOrDefaultAsync(s => s.Id == sg.StudentId && !s.IsDeleted);
                    if (studentToUpdate != null)
                    {
                        studentToUpdate.LastPaymentDate = DateTime.UtcNow;
                        studentToUpdate.TotalPaid += amountToCharge;
                        studentToUpdate.UpdatedAt = DateTimeOffset.UtcNow;
                        db.Students.Update(studentToUpdate);
                    }

                    successCount++;

                    try
                    {
                        var studentPhone = sg.Student?.PhoneNumber;
                        var studentName = sg.Student?.FullName ?? "донишҷӯ";
                        var groupName = sg.Group.Name;
                        if (!string.IsNullOrWhiteSpace(studentPhone))
                        {
                            var sms = $"Салом, {studentName}! Аз ҳисоби шумо барои гурӯҳи {groupName} {amountToCharge:0.##} сомонӣ гирифта шуд. Тавозуни боқимонда: {account.Balance:0.##} сомонӣ.";
                            await messageSenderService.SendSmsToNumberAsync(studentPhone, sms);
                        }
                    }
                    catch { }
                }
                else
                {
                    await NotifyInsufficientAsync(sg.StudentId, sg.GroupId, account, amountToCharge, date, sg.Group.Name);
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

    public async Task<Response<int>> RunMonthlyChargeForGroupAsync(int groupId, int month, int year)
    {
        try
        {
            var boundaryNow = DateTimeOffset.UtcNow;
            var group = await db.Groups
                .Include(g => g.Course)
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null || group.Status != ActiveStatus.Active || group.EndDate <= boundaryNow)
                return new Response<int>(HttpStatusCode.BadRequest, "Гурӯҳ фаъол нест ё ёфт нашуд");

            var sgs = await db.StudentGroups
                .Include(sg => sg.Student)
                .Where(sg => sg.GroupId == groupId &&
                             sg.IsActive && !sg.IsDeleted)
                .ToListAsync();

            var success = 0;
            foreach (var sg in sgs)
            {
                var res = await ChargeForGroupAsync(sg.StudentId, groupId, month, year);
                if (res.StatusCode == (int)HttpStatusCode.OK)
                {
                    success++;
                }
            }

            return new Response<int>(success) { Message = $"Дебет барои гурӯҳ анҷом шуд, муваффақ: {success}" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "RunMonthlyChargeForGroupAsync ноком шуд GroupId={GroupId} {Month}.{Year}", groupId, month, year);
            return new Response<int>(HttpStatusCode.InternalServerError, "Хатои дохилӣ дар дебети гурӯҳ");
        }
    }

    public async Task<Response<string>> ChargeForGroupAsync(int studentId, int groupId, int month, int year)
    {
        try
        {
            Log.Information("ChargeForGroupAsync start | StudentId={StudentId} GroupId={GroupId} Period={Month}.{Year}", studentId, groupId, month, year);
            var alreadyPaid = await db.Payments.AnyAsync(p => !p.IsDeleted && p.StudentId == studentId && p.GroupId == groupId && p.Month == month && p.Year == year && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid));
            if (alreadyPaid)
            {
                Log.Information("ChargeForGroupAsync: already paid, skipping | StudentId={StudentId} GroupId={GroupId}", studentId, groupId);
                return new Response<string>("Пардохти ин моҳ аллакай сабт шудааст");
            }

            var account = await db.StudentAccounts.FirstOrDefaultAsync(a => a.StudentId == studentId && a.IsActive && !a.IsDeleted);
            if (account == null)
            {
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

            var group = await db.Groups.Include(g => g.Course).FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null || group.Status != ActiveStatus.Active || group.EndDate <= DateTimeOffset.UtcNow)
            {
                return new Response<string>(HttpStatusCode.BadRequest, "Гурӯҳ фаъол нест ё мӯҳлаташ гузаштааст");
            }

            var preview = await discountService.PreviewAsync(studentId, groupId, month, year);
            if (preview.StatusCode != (int)HttpStatusCode.OK || preview.Data == null)
            {
                Log.Warning("ChargeForGroupAsync: preview failed | Status={Status} Message={Message}", preview.StatusCode, preview.Message);
                return new Response<string>((HttpStatusCode)preview.StatusCode, preview.Message ?? "Preview ноком шуд");
            }

            var amountToCharge = preview.Data.PayableAmount;
            if (amountToCharge <= 0)
            {
                // nothing to charge, just recalc aggregate status
                await RecalculateStudentPaymentStatusAsync(studentId, month, year);
                Log.Information("ChargeForGroupAsync: zero payable, marked student paid | StudentId={StudentId}", studentId);
                return new Response<string>("Маблағи пардохт 0 аст (бо тахфиф)");
            }

            if (account.Balance < amountToCharge)
            {
                await NotifyInsufficientAsync(studentId, groupId, account, amountToCharge, new DateTime(year, month, 1), (await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId))?.Name ?? "гурӯҳ");
                Log.Warning("ChargeForGroupAsync: insufficient funds | StudentId={StudentId} Balance={Balance} Required={Required}", studentId, account.Balance, amountToCharge);
                return new Response<string>(HttpStatusCode.BadRequest, "Баланс нокифоя аст барои пардохти моҳона");
            }

            account.Balance -= amountToCharge;
            account.UpdatedAt = DateTimeOffset.UtcNow;

    
            var groupName2 = group?.Name ?? $"GroupId={groupId}";
            db.AccountLogs.Add(new AccountLog
            {
                AccountId = account.Id,
                Amount = -amountToCharge,
                Type = "MonthlyCharge",
                Note = $"{month:00}.{year} - Пардохт барои гурӯҳ {groupName2}",
                PerformedByUserId = null,
                PerformedByName = "Система",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            var payment = new Payment
            {
                StudentId = studentId,
                GroupId = groupId,
                OriginalAmount = preview.Data.OriginalAmount,
                DiscountAmount = preview.Data.DiscountAmount,
                Amount = amountToCharge,
                PaymentMethod = PaymentMethod.Other,
                TransactionId = null,
                Description = $"Пардохт барои гурӯҳ {groupName2} (аз ҳисоби донишҷӯ)",
                Status = PaymentStatus.Completed,
                PaymentDate = DateTime.UtcNow,
                CenterId = group?.Course?.CenterId,
                Month = month,
                Year = year,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Payments.Add(payment);

            var studentToUpdate = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (studentToUpdate != null)
            {
                studentToUpdate.LastPaymentDate = DateTime.UtcNow;
                studentToUpdate.TotalPaid += amountToCharge;
                studentToUpdate.UpdatedAt = DateTimeOffset.UtcNow;
                db.Students.Update(studentToUpdate);
            }

            await db.SaveChangesAsync();

            // recalc aggregate paid status for current period
            await RecalculateStudentPaymentStatusAsync(studentId, month, year);

            try
            {
                var studentPhone = (await db.Students.FirstOrDefaultAsync(s => s.Id == studentId))?.PhoneNumber;
                var studentName = (await db.Students.FirstOrDefaultAsync(s => s.Id == studentId))?.FullName ?? "донишҷӯ";
                var groupName = group?.Name ?? "гурӯҳ";
                if (!string.IsNullOrWhiteSpace(studentPhone))
                {
                    var sms = $"Салом, {studentName}! Аз ҳисоби шумо барои гурӯҳи {groupName} {amountToCharge:0.##} сомонӣ гирифта шуд. Тавозуни боқимонда: {account.Balance:0.##} сомонӣ.";
                    await messageSenderService.SendSmsToNumberAsync(studentPhone, sms);
                }
            }
            catch { }

            Log.Information("ChargeForGroupAsync: success | StudentId={StudentId} GroupId={GroupId} Amount={Amount}", studentId, groupId, amountToCharge);
            return new Response<string>("Пардохти моҳона барои гурӯҳ анҷом шуд");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ChargeForGroupAsync failed StudentId={StudentId} GroupId={GroupId} {Month}.{Year}", studentId, groupId, month, year);
            return new Response<string>(HttpStatusCode.InternalServerError, "Хатои дохилӣ дар пардохти гурӯҳ");
        }
    }

    public async Task<Response<string>> RecalculateStudentPaymentStatusCurrentAsync(int studentId)
    {
        var now = DateTime.UtcNow;
        return await RecalculateStudentPaymentStatusAsync(studentId, now.Month, now.Year);
    }

    public async Task<Response<string>> RecalculateStudentPaymentStatusAsync(int studentId, int month, int year)
    {
        try
        {
            var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student == null)
                return new Response<string>(HttpStatusCode.NotFound, "Донишҷӯ ёфт нашуд");

            var activeGroupIds = await db.StudentGroups
                .Where(sg => sg.StudentId == studentId && sg.IsActive && !sg.IsDeleted)
                .Select(sg => sg.GroupId)
                .ToListAsync();

            bool isPaid;
            if (activeGroupIds.Count == 0)
            {
                isPaid = true; // no active groups means nothing to pay
            }
            else
            {
                isPaid = true;
                foreach (var gid in activeGroupIds)
                {
                    var hasPayment = await db.Payments.AnyAsync(p => !p.IsDeleted && p.StudentId == studentId && p.GroupId == gid && p.Month == month && p.Year == year && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid));
                    if (!hasPayment)
                    {
                        var preview = await discountService.PreviewAsync(studentId, gid, month, year);
                        var net = preview.Data?.PayableAmount ?? decimal.MaxValue;
                        if (net > 0)
                        {
                            isPaid = false;
                            break;
                        }
                    }
                }
            }

            student.PaymentStatus = isPaid ? PaymentStatus.Completed : PaymentStatus.Pending;
            student.UpdatedAt = DateTimeOffset.UtcNow;
            db.Students.Update(student);
            await db.SaveChangesAsync();

            return new Response<string>("Ҳолати пардохти донишҷӯ аз рӯи ҳамаи гурӯҳҳо навсозӣ шуд");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "RecalculateStudentPaymentStatusAsync failed StudentId={StudentId} {Month}.{Year}", studentId, month, year);
            return new Response<string>(HttpStatusCode.InternalServerError, "Хатои дохилӣ дар ҳисобкунии ҳолати пардохт");
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

    private async Task NotifyInsufficientAsync(int studentId, int groupId, StudentAccount account, decimal required, DateTime dueDate, string groupName)
    {
        try
        {
            var studentGroup = await db.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == studentId && sg.GroupId == groupId && !sg.IsDeleted);
            
            if (studentGroup == null) return;

            var today = DateTime.UtcNow.Date;
            if (studentGroup.LastPaymentReminderSentDate.HasValue && 
                studentGroup.LastPaymentReminderSentDate.Value.Date == today)
            {
                Log.Information("SMS-и огоҳии норасоии маблағ аллакай имрӯз фиристода шудааст: StudentId={StudentId} GroupId={GroupId}", studentId, groupId);
                return;
            }

            var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student == null) return;

            var missing = required - account.Balance;
            var sms = $"Салом, {student.FullName}! Барои пардохти моҳонаи гурӯҳи {groupName} маблағи ҳисобатон нокифоя аст. Камбуд: {missing:0.##} сомонӣ. Лутфан бо коди ҳамён {account.AccountCode} ба админ муроҷиат карда ҳисоби худро пур кунед.";
            
            if (!string.IsNullOrWhiteSpace(student.PhoneNumber))
            {
                await messageSenderService.SendSmsToNumberAsync(student.PhoneNumber, sms);
                studentGroup.LastPaymentReminderSentDate = DateTime.UtcNow;
                db.StudentGroups.Update(studentGroup);
                await db.SaveChangesAsync();
                
                Log.Information("SMS-и огоҳии норасоии маблағ фиристода шуд: StudentId={StudentId} GroupId={GroupId}", studentId, groupId);
            }

            if (!string.IsNullOrWhiteSpace(student.Email))
            {
                await messageSenderService.SendEmailToAddressAsync(new Domain.DTOs.MessageSender.SendEmailToAddressDto
                {
                    EmailAddress = student.Email,
                    Subject = "Норасоии маблағ барои пардохти моҳона",
                    MessageContent = $"<p>Салом, {student.FullName}.</p><p>Барои пардохти моҳонаи гурӯҳи <b>{groupName}</b> дар {dueDate:MM.yyyy} маблағи ҳисобатон нокифоя аст.</p><p>Камбуд: <b>{missing:0.##}</b> сомонӣ.</p><p>Лутфан бо коди ҳамён <b>{account.AccountCode}</b> ба админ муроҷиат намуда, ҳисоби худро пур кунед.</p>"
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

    private async Task RetryPendingForStudentAsync(int studentId)
    {
        var now = DateTime.UtcNow;
        var month = now.Month;
        var year = now.Year;

        var account = await db.StudentAccounts.FirstOrDefaultAsync(a => a.StudentId == studentId && a.IsActive && !a.IsDeleted);
        if (account == null) return;

        var boundary = DateTimeOffset.UtcNow;
        var sgs = await db.StudentGroups
            .Include(sg => sg.Student)
            .Include(sg => sg.Group).ThenInclude(g => g.Course)
            .Where(sg => sg.StudentId == studentId &&
                         sg.IsActive && !sg.IsDeleted &&
                         sg.Group != null &&
                         !sg.Group.IsDeleted &&
                         sg.Group.Status == ActiveStatus.Active &&
                         sg.Group.EndDate > boundary)
            .ToListAsync();

        var anyInsufficient = false;
        var anySuccess = false;

        foreach (var sg in sgs)
        {
            // skip if payment already exists for month
            var alreadyPaid = await db.Payments.AnyAsync(p => !p.IsDeleted && p.StudentId == sg.StudentId && p.GroupId == sg.GroupId && p.Month == month && p.Year == year);
            if (alreadyPaid) continue;

            var preview = await discountService.PreviewAsync(sg.StudentId, sg.GroupId, month, year);
            if (preview.StatusCode != (int)HttpStatusCode.OK || preview.Data == null) continue;

            var amountToCharge = preview.Data.PayableAmount;
            if (amountToCharge <= 0) continue;

            if (account.Balance >= amountToCharge)
            {
                account.Balance -= amountToCharge;
                account.UpdatedAt = DateTimeOffset.UtcNow;

                var groupName3 = sg.Group.Name;
                db.AccountLogs.Add(new AccountLog
                {
                    AccountId = account.Id,
                    Amount = -amountToCharge,
                    Type = "MonthlyCharge",
                    Note = $"{month:00}.{year} - Пардохт барои гурӯҳ {groupName3}",
                    PerformedByUserId = null,
                    PerformedByName = "Система",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });

                db.Payments.Add(new Payment
                {
                    StudentId = sg.StudentId,
                    GroupId = sg.GroupId,
                    OriginalAmount = preview.Data.OriginalAmount,
                    DiscountAmount = preview.Data.DiscountAmount,
                    Amount = amountToCharge,
                    PaymentMethod = PaymentMethod.Other,
                    TransactionId = null,
                    Description = $"Пардохт барои гурӯҳ {groupName3} (аз ҳисоби донишҷӯ)",
                    Status = PaymentStatus.Completed,
                    PaymentDate = DateTime.UtcNow,
                    CenterId = sg.Group.Course.CenterId,
                    Month = month,
                    Year = year,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });

                // sync student's stats (status will be recalculated)
                var studentToUpdate2 = await db.Students.FirstOrDefaultAsync(s => s.Id == sg.StudentId && !s.IsDeleted);
                if (studentToUpdate2 != null)
                {
                    studentToUpdate2.LastPaymentDate = DateTime.UtcNow;
                    studentToUpdate2.TotalPaid += amountToCharge;
                    studentToUpdate2.UpdatedAt = DateTimeOffset.UtcNow;
                    db.Students.Update(studentToUpdate2);
                }

                anySuccess = true;

                try
                {
                    var studentPhone = sg.Student?.PhoneNumber;
                    var studentName = sg.Student?.FullName ?? "донишҷӯ";
                    var groupName = sg.Group.Name;
                    if (!string.IsNullOrWhiteSpace(studentPhone))
                    {
                        var sms = $"Салом, {studentName}! Аз ҳисоби шумо барои гурӯҳи {groupName} {amountToCharge:0.##} сомонӣ гирифта шуд. Тавозуни боқимонда: {account.Balance:0.##} сомонӣ.";
                        await messageSenderService.SendSmsToNumberAsync(studentPhone, sms);
                    }
                }
                catch { }
            }
            else
            {
                anyInsufficient = true;
                await NotifyInsufficientAsync(sg.StudentId, sg.GroupId, account, amountToCharge, new DateTime(year, month, 1), sg.Group.Name);
            }
        }

        await db.SaveChangesAsync();
        await RecalculateStudentPaymentStatusAsync(studentId, month, year);
    }
}


