using System.Net;
using Domain.DTOs.Finance;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Constants;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Domain.Filters;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Services;

    public class StudentAccountService(DataContext db,
        IDiscountService discountService, 
        IMessageSenderService messageSenderService,
        IHttpContextAccessor httpContextAccessor) : IStudentAccountService
{
    #region GetByStudentIdAsync

    public async Task<Response<GetStudentAccountDto>> GetByStudentIdAsync(int studentId)
    {
        var account = await db.StudentAccounts.FirstOrDefaultAsync(a => a.StudentId == studentId && !a.IsDeleted);
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

        return new Response<GetStudentAccountDto>(Map(account));
    }

    #endregion

    #region GetLastLogsAsync

    public async Task<Response<List<GetAccountLogDto>>> GetLastLogsAsync(int studentId, int limit = 10)
    {
        try
        {
            var account = await db.StudentAccounts.FirstOrDefaultAsync(a => a.StudentId == studentId && !a.IsDeleted);
            if (account == null)
            {
                return new Response<List<GetAccountLogDto>>(HttpStatusCode.OK, Messages.StudentAccount.AccountNotCreated)
                {
                    Data = new List<GetAccountLogDto>()
                };
            }

            var effectiveLimit = limit <= 0 ? 10 : limit;

            var logs = await db.AccountLogs
                .Where(l => l.AccountId == account.Id && !l.IsDeleted)
                .OrderByDescending(l => l.CreatedAt)
                .Take(effectiveLimit)
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
                Message = Messages.StudentAccount.LogsLoaded
            };
        }
        catch (Exception ex)
        {
            var fullName = await db.Students.Where(s => s.Id == studentId).Select(s => s.FullName).FirstOrDefaultAsync();
            Log.Error(ex, "Failed to load account logs for student {FullName}", fullName ?? "unknown");
            return new Response<List<GetAccountLogDto>>(HttpStatusCode.InternalServerError, Messages.Common.InternalError);
        }
    }

    #endregion

    #region GetAccountsAsync

    public async Task<PaginationResponse<List<AccountListItemDto>>> GetAccountsAsync(string? search, int pageNumber, int pageSize)
    {
        var query = db.StudentAccounts.AsNoTracking()
            .Include(a => a.Student)
            .Where(a => !a.IsDeleted && a.Student != null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => EF.Functions.ILike(a.Student!.FullName, $"%{search.Trim()}%"));
        }

        var page = pageNumber <= 0 ? 1 : pageNumber;
        var size = pageSize <= 0 ? 10 : pageSize;

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.Student!.FullName)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(a => new AccountListItemDto
            {
                StudentId = a.StudentId,
                FullName = a.Student!.FullName,
                AccountCode = a.AccountCode
            })
            .ToListAsync();

        return new PaginationResponse<List<AccountListItemDto>>(items, total, page, size);
    }

    #endregion

    #region GetMyWalletAsync

    public async Task<Response<MyWalletDto>> GetMyWalletAsync(int limit = 10)
    {
        try
        {
            var user = httpContextAccessor.HttpContext?.User;
            var idStr = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out var userId))
                return new Response<MyWalletDto>(HttpStatusCode.Unauthorized, Messages.User.UserNotAuthenticated);

            var student = await db.Students.FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);
            if (student == null)
                return new Response<MyWalletDto>(HttpStatusCode.NotFound, Messages.Student.NotFound);

            var accountResp = await GetByStudentIdAsync(student.Id);
            if (accountResp.Data == null)
                return new Response<MyWalletDto>((HttpStatusCode)accountResp.StatusCode, accountResp.Message ?? Messages.StudentAccount.NotFound);

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
            return new Response<MyWalletDto>(HttpStatusCode.InternalServerError, Messages.Common.InternalError);
        }
    }

    #endregion

    #region TopUpAsync

    public async Task<Response<GetStudentAccountDto>> TopUpAsync(TopUpDto dto)
    {
        try
        {
            var account = await db.StudentAccounts.FirstOrDefaultAsync(a => a.AccountCode == dto.AccountCode && a.IsActive && !a.IsDeleted);
            if (account == null)
                return new Response<GetStudentAccountDto>(HttpStatusCode.NotFound, Messages.StudentAccount.AccountNotActive);

            if (dto.Amount <= 0)
                return new Response<GetStudentAccountDto>(HttpStatusCode.BadRequest, Messages.StudentAccount.AmountMustBePositive);

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
                    var smsText = string.Format(Messages.Sms.TopUpNotification, student.FullName, dto.Amount, account.Balance);
                    await messageSenderService.SendSmsToNumberAsync(student.PhoneNumber, smsText);
                }

                await RetryPendingForStudentAsync(account.StudentId);
            }
            catch
            {
                //
            }

            Log.Information("TopUp: AccountId={AccountId} Amount={Amount}", account.Id, dto.Amount);
            return new Response<GetStudentAccountDto>(Map(account)) { Message = Messages.StudentAccount.TopUpSuccess };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "TopUp failed for AccountCode={AccountCode}", dto.AccountCode);
            return new Response<GetStudentAccountDto>(HttpStatusCode.InternalServerError, Messages.Common.InternalError);
        }
    }

    #endregion

    #region WithdrawAsync

    public async Task<Response<GetStudentAccountDto>> WithdrawAsync(WithdrawDto dto)
    {
        try
        {
            var account = await db.StudentAccounts.FirstOrDefaultAsync(a => a.AccountCode == dto.StudentAccount && a.IsActive && !a.IsDeleted);
            if (account == null)
                return new Response<GetStudentAccountDto>(HttpStatusCode.NotFound, Messages.StudentAccount.NotFound);

            if (dto.Amount <= 0)
                return new Response<GetStudentAccountDto>(HttpStatusCode.BadRequest, Messages.StudentAccount.AmountMustBePositive);

            if (account.Balance < dto.Amount)
                return new Response<GetStudentAccountDto>(HttpStatusCode.BadRequest, string.Format(Messages.StudentAccount.InsufficientBalanceDetails, account.Balance));

            account.Balance -= dto.Amount;
            account.UpdatedAt = DateTimeOffset.UtcNow;

            var (userId, userName) = GetCurrentUser();

            var log = new AccountLog
            {
                AccountId = account.Id,
                Amount = -dto.Amount,
                Type = "Withdraw",
                Note = dto.Reason ?? "Withdraw",
                PerformedByUserId = userId,
                PerformedByName = userName,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            db.AccountLogs.Add(log);
            await db.SaveChangesAsync();

            Log.Information("Withdraw: AccountId={AccountId} Amount={Amount} StudentAccount={StudentAccount}", account.Id, dto.Amount, dto.StudentAccount);
            return new Response<GetStudentAccountDto>(Map(account)) { Message = Messages.StudentAccount.WithdrawSuccess };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Withdraw failed StudentAccount={StudentAccount}", dto.StudentAccount);
            return new Response<GetStudentAccountDto>(HttpStatusCode.InternalServerError, Messages.Common.InternalError);
        }
    }

    #endregion

    #region RunMonthlyChargeAsync

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
                    continue;

                var preview = await discountService.PreviewAsync(sg.StudentId, sg.GroupId, month, year);
                if (preview.StatusCode != (int)HttpStatusCode.OK || preview.Data == null)
                    continue;

                var amountToCharge = preview.Data.PayableAmount;
                if (amountToCharge <= 0)
                    continue;

                if (account.Balance >= amountToCharge)
                {
                    account.Balance -= amountToCharge;
                    account.UpdatedAt = DateTimeOffset.UtcNow;

                    var groupName = sg.Group.Name;
                    db.AccountLogs.Add(new AccountLog
                    {
                        AccountId = account.Id,
                        Amount = -amountToCharge,
                        Type = "MonthlyCharge",
                        Note = $"{month:00}.{year} - Оплата за группу {groupName}",
                        PerformedByUserId = null,
                        PerformedByName = "System",
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
                        Description = $"Оплата за группу {groupName} (со счета студента)",
                        Status = PaymentStatus.Completed,
                        PaymentDate = DateTime.UtcNow,
                        CenterId = sg.Group.Course.CenterId,
                        Month = month,
                        Year = year,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    });

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
                        var studentName = sg.Student?.FullName ?? "Студент";
                        if (!string.IsNullOrWhiteSpace(studentPhone))
                        {
                            var sms = string.Format(Messages.Sms.ChargeNotification, studentName, amountToCharge, groupName, account.Balance);
                            await messageSenderService.SendSmsToNumberAsync(studentPhone, sms);
                        }
                    }
                    catch
                    {
                    }
                }
                else
                {
                    await NotifyInsufficientAsync(sg.StudentId, sg.GroupId, account, amountToCharge, date, sg.Group.Name);
                }
            }

            await db.SaveChangesAsync();

            return new Response<int>(successCount)
            {
                Message = string.Format(Messages.StudentAccount.MonthlyChargeSummary, successCount)
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Monthly charge failed {Month}.{Year}", month, year);
            return new Response<int>(HttpStatusCode.InternalServerError, Messages.Common.InternalError);
        }
    }

    #endregion

    #region RunMonthlyChargeForGroupAsync

    public async Task<Response<int>> RunMonthlyChargeForGroupAsync(int groupId, int month, int year)
    {
        try
        {
            var boundaryNow = DateTimeOffset.UtcNow;
            var group = await db.Groups
                .Include(g => g.Course)
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null || group.Status != ActiveStatus.Active || group.EndDate <= boundaryNow)
                return new Response<int>(HttpStatusCode.BadRequest, Messages.StudentAccount.GroupInactive);

            var sgs = await db.StudentGroups
                .Include(sg => sg.Student)
                .Where(sg => sg.GroupId == groupId && sg.IsActive && !sg.IsDeleted)
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

            return new Response<int>(success)
            {
                Message = string.Format(Messages.StudentAccount.GroupChargeSummary, success)
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "RunMonthlyChargeForGroupAsync failed GroupId={GroupId} {Month}.{Year}", groupId, month, year);
            return new Response<int>(HttpStatusCode.InternalServerError, Messages.Common.InternalError);
        }
    }

    #endregion

    #region ChargeForGroupAsync

    public async Task<Response<string>> ChargeForGroupAsync(int studentId, int groupId, int month, int year)
    {
        try
        {
            Log.Information("ChargeForGroupAsync start | StudentId={StudentId} GroupId={GroupId} Period={Month}.{Year}", studentId, groupId, month, year);
            var alreadyPaid = await db.Payments.AnyAsync(p => !p.IsDeleted && p.StudentId == studentId && p.GroupId == groupId && p.Month == month && p.Year == year && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid));
            if (alreadyPaid)
            {
                Log.Information("ChargeForGroupAsync: already paid, skipping | StudentId={StudentId} GroupId={GroupId}", studentId, groupId);
                return new Response<string>(Messages.StudentAccount.AlreadyPaid);
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
                return new Response<string>(HttpStatusCode.BadRequest, Messages.StudentAccount.GroupInactive);
            }

            var preview = await discountService.PreviewAsync(studentId, groupId, month, year);
            if (preview.StatusCode != (int)HttpStatusCode.OK || preview.Data == null)
            {
                Log.Warning("ChargeForGroupAsync: preview failed | Status={Status} Message={Message}", preview.StatusCode, preview.Message);
                return new Response<string>((HttpStatusCode)preview.StatusCode, preview.Message ?? Messages.Common.InternalError);
            }

            var amountToCharge = preview.Data.PayableAmount;
            if (amountToCharge <= 0)
            {
                await RecalculateStudentPaymentStatusAsync(studentId, month, year);
                Log.Information("ChargeForGroupAsync: zero payable, marked student paid | StudentId={StudentId}", studentId);
                return new Response<string>(Messages.StudentAccount.ZeroPayable);
            }

            if (account.Balance < amountToCharge)
            {
                var groupName = (await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId))?.Name ?? "Группа";
                await NotifyInsufficientAsync(studentId, groupId, account, amountToCharge, new DateTime(year, month, 1), groupName);
                Log.Warning("ChargeForGroupAsync: insufficient funds | StudentId={StudentId} Balance={Balance} Required={Required}", studentId, account.Balance, amountToCharge);
                return new Response<string>(HttpStatusCode.BadRequest, Messages.StudentAccount.InsufficientBalance);
            }

            account.Balance -= amountToCharge;
            account.UpdatedAt = DateTimeOffset.UtcNow;

            var groupNameLabel = group?.Name ?? $"GroupId={groupId}";
            db.AccountLogs.Add(new AccountLog
            {
                AccountId = account.Id,
                Amount = -amountToCharge,
                Type = "MonthlyCharge",
                Note = $"{month:00}.{year} - Оплата за группу {groupNameLabel}",
                PerformedByUserId = null,
                PerformedByName = "System",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            db.Payments.Add(new Payment
            {
                StudentId = studentId,
                GroupId = groupId,
                OriginalAmount = preview.Data.OriginalAmount,
                DiscountAmount = preview.Data.DiscountAmount,
                Amount = amountToCharge,
                PaymentMethod = PaymentMethod.Other,
                TransactionId = null,
                Description = $"Оплата за группу {groupNameLabel} (со счета студента)",
                Status = PaymentStatus.Completed,
                PaymentDate = DateTime.UtcNow,
                CenterId = group?.Course?.CenterId,
                Month = month,
                Year = year,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            var studentToUpdate = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (studentToUpdate != null)
            {
                studentToUpdate.LastPaymentDate = DateTime.UtcNow;
                studentToUpdate.TotalPaid += amountToCharge;
                studentToUpdate.UpdatedAt = DateTimeOffset.UtcNow;
                db.Students.Update(studentToUpdate);
            }

            await db.SaveChangesAsync();
            await RecalculateStudentPaymentStatusAsync(studentId, month, year);

            try
            {
                var studentInfo = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
                var studentPhone = studentInfo?.PhoneNumber;
                var studentName = studentInfo?.FullName ?? "Студент";
                var groupName = group?.Name ?? "Группа";
                if (!string.IsNullOrWhiteSpace(studentPhone))
                {
                    var sms = string.Format(Messages.Sms.ChargeNotification, studentName, amountToCharge, groupName, account.Balance);
                    await messageSenderService.SendSmsToNumberAsync(studentPhone, sms);
                }
            }
            catch
            {
            }

            Log.Information("ChargeForGroupAsync: success | StudentId={StudentId} GroupId={GroupId} Amount={Amount}", studentId, groupId, amountToCharge);
            return new Response<string>(Messages.StudentAccount.ChargeSuccess);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ChargeForGroupAsync failed StudentId={StudentId} GroupId={GroupId} {Month}.{Year}", studentId, groupId, month, year);
            return new Response<string>(HttpStatusCode.InternalServerError, Messages.StudentAccount.ChargeFailed);
        }
    }

    #endregion

    #region RecalculateStudentPaymentStatusCurrentAsync

    public async Task<Response<string>> RecalculateStudentPaymentStatusCurrentAsync(int studentId)
    {
        var now = DateTime.UtcNow;
        return await RecalculateStudentPaymentStatusAsync(studentId, now.Month, now.Year);
    }

    #endregion

    #region RecalculateStudentPaymentStatusAsync

    public async Task<Response<string>> RecalculateStudentPaymentStatusAsync(int studentId, int month, int year)
    {
        try
        {
            var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Student.NotFound);

            var activeGroupIds = await db.StudentGroups
                .Where(sg => sg.StudentId == studentId && sg.IsActive && !sg.IsDeleted)
                .Select(sg => sg.GroupId)
                .ToListAsync();

            var isPaid = true;
            if (activeGroupIds.Count > 0)
            {
                foreach (var gid in activeGroupIds)
                {
                    var hasPayment = await db.Payments.AnyAsync(p => !p.IsDeleted && p.StudentId == studentId && p.GroupId == gid && p.Month == month && p.Year == year && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid));
                    if (!hasPayment)
                    {
                        var preview = await discountService.PreviewAsync(studentId, gid, month, year);
                        var net = preview.Data.PayableAmount;
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

            return new Response<string>(Messages.StudentAccount.PaymentStatusRecalculated);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "RecalculateStudentPaymentStatusAsync failed StudentId={StudentId} {Month}.{Year}", studentId, month, year);
            return new Response<string>(HttpStatusCode.InternalServerError, Messages.Common.InternalError);
        }
    }

    #endregion

    #region GenerateUniqueCodeAsync

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

    #endregion

    #region NotifyInsufficientAsync

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
            if (!string.IsNullOrWhiteSpace(student.PhoneNumber))
            {
                var sms = string.Format(Messages.Sms.InsufficientFunds, student.FullName, groupName, dueDate, missing, account.AccountCode);
                await messageSenderService.SendSmsToNumberAsync(student.PhoneNumber, sms);
                studentGroup.LastPaymentReminderSentDate = DateTime.UtcNow;
                db.StudentGroups.Update(studentGroup);
                await db.SaveChangesAsync();

                Log.Information("Insufficient balance SMS sent: StudentId={StudentId} GroupId={GroupId}", studentId, groupId);
            }

            if (!string.IsNullOrWhiteSpace(student.Email))
            {
                await messageSenderService.SendEmailToAddressAsync(new Domain.DTOs.MessageSender.SendEmailToAddressDto
                {
                    EmailAddress = student.Email,
                    Subject = Messages.StudentAccount.InsufficientBalance,
                    MessageContent = string.Format(Messages.Email.InsufficientFunds, student.FullName, groupName, dueDate, missing, account.AccountCode)
                });
            }
        }
        catch (Exception ex)
        {
            var fullName = await db.Students.Where(s => s.Id == studentId).Select(s => s.FullName).FirstOrDefaultAsync();
            Log.Warning(ex, "Insufficient notification failed for student {FullName}", fullName ?? "unknown");
        }
    }

    #endregion

    #region Map

    private static GetStudentAccountDto Map(StudentAccount a) => new()
    {
        Id = a.Id,
        StudentId = a.StudentId,
        AccountCode = a.AccountCode,
        Balance = a.Balance,
        IsActive = a.IsActive
    };

    #endregion

    #region GetCurrentUser

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

    #endregion

    #region TranslateType

    private static string TranslateType(string type)
    {
        return type switch
        {
            "TopUp" => "Пополнение",
            "MonthlyCharge" => "Ежемесячное списание",
            "Refund" => "Возврат",
            "Adjustment" => "Корректировка",
            _ => type
        };
    }

    #endregion

    #region RetryPendingForStudentAsync

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

        foreach (var sg in sgs)
        {
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
                    Note = $"{month:00}.{year} - Оплата за группу {groupName3}",
                    PerformedByUserId = null,
                    PerformedByName = "System",
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
                    Description = $"Оплата за группу {groupName3} (со счета студента)",
                    Status = PaymentStatus.Completed,
                    PaymentDate = DateTime.UtcNow,
                    CenterId = sg.Group.Course.CenterId,
                    Month = month,
                    Year = year,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });

                var studentToUpdate2 = await db.Students.FirstOrDefaultAsync(s => s.Id == sg.StudentId && !s.IsDeleted);
                if (studentToUpdate2 != null)
                {
                    studentToUpdate2.LastPaymentDate = DateTime.UtcNow;
                    studentToUpdate2.TotalPaid += amountToCharge;
                    studentToUpdate2.UpdatedAt = DateTimeOffset.UtcNow;
                    db.Students.Update(studentToUpdate2);
                }

                try
                {
                    var studentPhone = sg.Student?.PhoneNumber;
                    var studentName = sg.Student?.FullName ?? "Студент";
                    var groupName = sg.Group.Name;
                    if (!string.IsNullOrWhiteSpace(studentPhone))
                    {
                        var sms = string.Format(Messages.Sms.ChargeNotification, studentName, amountToCharge, groupName, account.Balance);
                        await messageSenderService.SendSmsToNumberAsync(studentPhone, sms);
                    }
                }
                catch
                {
                }
            }
            else
            {
                await NotifyInsufficientAsync(sg.StudentId, sg.GroupId, account, amountToCharge, new DateTime(year, month, 1), sg.Group.Name);
            }
        }

        await db.SaveChangesAsync();
        await RecalculateStudentPaymentStatusAsync(studentId, month, year);
    }

    #endregion
}


