using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Infrastructure.Interfaces;
using Serilog;

namespace Infrastructure.BackgroundTasks;

public class WalletBackfillService(
    DataContext db,
    IOsonSmsService smsService
)
{
    public async Task<int> EnsureWalletsForAllStudentsAsync()
    {
        try
        {
            // Find students who do NOT have a StudentAccount
            var missing = await db.Students
                .Where(s => !s.IsDeleted && !db.StudentAccounts.Any(a => !a.IsDeleted && a.StudentId == s.Id))
                .Select(s => new { s.Id, s.FullName, s.PhoneNumber })
                .ToListAsync();

            if (missing.Count == 0) return 0;

            var created = new List<StudentAccount>(missing.Count);
            foreach (var s in missing)
            {
                var code = await GenerateUniqueCodeAsync();
                created.Add(new StudentAccount
                {
                    StudentId = s.Id,
                    AccountCode = code,
                    Balance = 0,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }

            await db.StudentAccounts.AddRangeAsync(created);
            await db.SaveChangesAsync();

            // Send SMS with code
            foreach (var sa in created)
            {
                try
                {
                    var phone = await db.Students.Where(x => x.Id == sa.StudentId)
                        .Select(x => new { x.PhoneNumber, x.FullName })
                        .FirstOrDefaultAsync();
                    if (phone != null && !string.IsNullOrWhiteSpace(phone.PhoneNumber))
                    {
                        var sms = $"Салом, {phone.FullName}! Барои шумо ҳамёни донишҷӯ эҷод шуд. Код: {sa.AccountCode}. Ин кодро ҳангоми пур кардани ҳисоб ҳатман ба админ нишон диҳед. Лутфан рамзро нигоҳ доред ва гум накунед.";
                        await smsService.SendSmsAsync(phone.PhoneNumber, sms);
                    }
                }
                catch { }
            }

            Log.Information("WalletBackfill: created {Count} wallets for legacy students", created.Count);
            return created.Count;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "WalletBackfill ноком шуд");
            return 0;
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
}


