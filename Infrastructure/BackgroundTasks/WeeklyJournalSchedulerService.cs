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
        var target = new TimeSpan(0, 5, 0); // 05:00 AM Dushanbe time
        var candidate = new DateTimeOffset(local.Date.Add(target), local.Offset);
        
        // Агар вақти ҷорӣ аз вақти мақсад калонтар бошад, ба рӯзи навбатӣ гузарем
        if (local >= candidate)
            candidate = candidate.AddDays(1);
            
        return candidate;
    }

    public async Task<BackgroundTaskResult> ProcessActiveGroupsAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var journalService = scope.ServiceProvider.GetRequiredService<IJournalService>();
        
        var groups = await context.Groups
            .Where(g => !g.IsDeleted && g.Status == ActiveStatus.Active)
            .ToListAsync(ct);

        var result = new BackgroundTaskResult();

        if (groups.Count == 0)
        {
            logger.LogInformation("Активные группы для планирования не найдены");
            result.Messages.Add("No active groups");
            return result;
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
                    var ok = res.StatusCode >= 200 && res.StatusCode < 300;
                    if (ok)
                    {
                        result.SuccessCount++;
                        result.Messages.Add($"Created first journal for group {group.Id}");
                    }
                    else
                    {
                        result.FailedCount++;
                        result.FailedItems.Add(group.Id.ToString());
                        result.Messages.Add($"Failed to create first journal for group {group.Name}: {res.Message}");
                    }
                    logger.LogInformation("Создана первая журнальная неделя для группы {Name}: статус {status}", group.Name, res.StatusCode);
                    continue;
                }

                // Санҷидани ки ҳафтаи навбатӣ лозим аст
                if (latestJournal.WeekNumber < group.TotalWeeks)
                {
                    // Ҳисобкунии вақти анҷоми ҳафтаи ҷорӣ дар timezone-и Душанбе
                    var weekEndTimeLocal = latestJournal.WeekEndDate.ToDushanbeTime();
                    var currentTimeLocal = DateTimeOffset.UtcNow.ToDushanbeTime();
                    
                    // Агар вақти ҷорӣ аз вақти анҷоми ҳафта калонтар бошад, ҳафтаи навбатӣ лозим аст
                    if (currentTimeLocal > weekEndTimeLocal)
                    {
                        var nextWeek = latestJournal.WeekNumber + 1;
                        var existsNext = await context.Journals
                            .AnyAsync(j => j.GroupId == group.Id && j.WeekNumber == nextWeek && !j.IsDeleted, ct);
                        if (!existsNext)
                        {
                            var res = await journalService.GenerateWeeklyJournalAsync(group.Id, nextWeek);
                            var ok = res.StatusCode >= 200 && res.StatusCode < 300;
                            if (ok)
                            {
                                result.SuccessCount++;
                                result.Messages.Add($"Created week {nextWeek} for group {group.Id}");
                            }
                            else
                            {
                                result.FailedCount++;
                                result.FailedItems.Add(group.Id.ToString());
                                result.Messages.Add($"Failed to create week {nextWeek} for group {group.Name}: {res.Message}");
                            }
                            logger.LogInformation(
                                "Автосоздана следующая неделя {week} для группы {GroupName}: статус {status}", nextWeek,
                                group.Name, res.StatusCode);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.FailedItems.Add(group.Id.ToString());
                result.Messages.Add($"Exception processing group {group.Name}: {ex.Message}");
                logger.LogError(ex, "Ошибка обработки группы {GroupName} в WeeklyJournalSchedulerService", group.Name);
            }
        }

        logger.LogInformation("WeeklyJournalSchedulerService finished run: {result}", result);
        return result;
    }
}