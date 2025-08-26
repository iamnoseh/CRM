using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundTasks;

public class WeeklyJournalSchedulerService(
    ILogger<WeeklyJournalSchedulerService> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Служба планирования недельных журналов запущена");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nowUtc = DateTimeOffset.UtcNow;
                var nextRun = CalculateNextRunTime(nowUtc);
                var delay = nextRun - nowUtc;
                logger.LogInformation("Следующий запуск планировщика журналов: {time}", nextRun.ToDushanbeTime());
                await Task.Delay(delay, stoppingToken);
                await ProcessActiveGroupsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка в цикле WeeklyJournalSchedulerService");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private static DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
    {
        var local = currentTime.ToDushanbeTime();
        var target = new TimeSpan(0, 5, 0);
        var candidate = new DateTimeOffset(local.Date.Add(target), local.Offset);
        if (local >= candidate)
            candidate = candidate.AddDays(1);
        return candidate;
    }

    public async Task ProcessActiveGroupsAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var journalService = scope.ServiceProvider.GetRequiredService<IJournalService>();
        
        var groups = await context.Groups
            .Where(g => !g.IsDeleted && g.Status == ActiveStatus.Active)
            .ToListAsync(ct);

        if (groups.Count == 0)
        {
            logger.LogInformation("Активные группы для планирования не найдены");
            return;
        }

        foreach (var group in groups)
        {
            try
            {
                var latestJournal = await context.Journals
                    .Where(j => j.GroupId == group.Id && !j.IsDeleted)
                    .OrderByDescending(j => j.WeekNumber)
                    .FirstOrDefaultAsync(ct);

                if (latestJournal == null)
                {
                    var res = await journalService.GenerateWeeklyJournalAsync(group.Id, 1);
                    logger.LogInformation("Создана первая журнальная неделя для группы {groupId}: статус {status}", group.Id, res.StatusCode);
                    continue;
                }

                if (latestJournal.WeekNumber < group.TotalWeeks && DateTimeOffset.UtcNow > latestJournal.WeekEndDate)
                {
                    var nextWeek = latestJournal.WeekNumber + 1;
                    var existsNext = await context.Journals
                        .AnyAsync(j => j.GroupId == group.Id && j.WeekNumber == nextWeek && !j.IsDeleted, ct);
                    if (!existsNext)
                    {
                        var res = await journalService.GenerateWeeklyJournalAsync(group.Id, nextWeek);
                        logger.LogInformation(
                            "Автосоздана следующая неделя {week} для группы {groupId}: статус {status}", nextWeek,
                            group.Id, res.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка обработки группы {groupId} в WeeklyJournalSchedulerService", group.Id);
            }
        }
    }
}