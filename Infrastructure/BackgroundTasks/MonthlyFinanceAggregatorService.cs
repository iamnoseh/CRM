using Infrastructure.Interfaces;
using Infrastructure.Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.BackgroundTasks;

public class MonthlyFinanceAggregatorService(ILogger<MonthlyFinanceAggregatorService> logger, IServiceProvider serviceProvider)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Служба ежемесячной финансовой агрегации запущена");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var next = CalculateNextRunTime(now);
                var delay = next - now;
                logger.LogInformation("Следующий запуск финансовой агрегации: {time}", next);
                await Task.Delay(delay, stoppingToken);
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка в цикле MonthlyFinanceAggregatorService");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private static DateTimeOffset CalculateNextRunTime(DateTimeOffset current)
    {
        var firstOfNextMonth = new DateTimeOffset(new DateTime(current.Year, current.Month, 1, 0, 5, 0, DateTimeKind.Utc)).AddMonths(1);
        return firstOfNextMonth;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var period = now.AddMonths(-1);
            var year = period.Year;
            var month = period.Month;

            var centers = await db.Centers.AsNoTracking().Select(c => new { c.Id }).ToListAsync(ct);
            foreach (var c in centers)
            {
                var income = await db.Payments.AsNoTracking()
                    .Where(p => p.CenterId == c.Id && p.Year == year && p.Month == month && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid))
                    .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

                var expense = await db.Expenses.AsNoTracking()
                    .Where(e => e.CenterId == c.Id && e.Year == year && e.Month == month && !e.IsDeleted)
                    .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

                var existing = await db.MonthlyFinancialSummaries.FirstOrDefaultAsync(m => m.CenterId == c.Id && m.Year == year && m.Month == month, ct);
                if (existing is null)
                {
                    db.MonthlyFinancialSummaries.Add(new MonthlyFinancialSummary
                    {
                        CenterId = c.Id,
                        Year = year,
                        Month = month,
                        TotalIncome = income,
                        TotalExpense = expense,
                        NetProfit = income - expense,
                        GeneratedDate = DateTimeOffset.UtcNow,
                        IsClosed = false,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    });
                }
                else
                {
                    existing.TotalIncome = income;
                    existing.TotalExpense = expense;
                    existing.NetProfit = income - expense;
                    existing.GeneratedDate = DateTimeOffset.UtcNow;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Monthly finance aggregation completed for {Year}-{Month}", year, month);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка выполнения MonthlyFinanceAggregatorService");
        }
    }
}


