using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Domain.Responses;
using System.Net;

namespace Infrastructure.BackgroundTasks;

public class GroupExpirationService(
    ILogger<GroupExpirationService> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    public async Task Run()
    {
        try
        {
            var localNow = DateTimeOffset.UtcNow.ToDushanbeTime();
            logger.LogInformation("Проверка статуса истечения групп в {time}", localNow);
            var resp = await CheckExpiredGroups();
            logger.LogInformation("CheckExpiredGroups result: {msg}", resp.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while checking group expiration: {message}", ex.Message);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Служба проверки истечения групп запущена");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var nextRunTime = CalculateNextRunTime(now);
                var delay = nextRunTime - now;
                if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(1);

                logger.LogInformation($"Следующая проверка истечения групп запланирована на {nextRunTime.ToDushanbeTime()} (через {delay.TotalHours:F1} часов)");
                await Task.Delay(delay, stoppingToken);
                var resp = await CheckExpiredGroups();
                logger.LogInformation("CheckExpiredGroups result: {msg}", resp.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при проверке истечения групп");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
    {
        var localTime = currentTime.ToDushanbeTime();
        var targetTime = new TimeSpan(0, 7, 0); // 00:07 AM
        var targetRunTime = localTime.Date.Add(targetTime);
        var targetDateTimeOffset = new DateTimeOffset(targetRunTime, localTime.Offset);

        if (localTime >= targetDateTimeOffset)
        {
            targetDateTimeOffset = targetDateTimeOffset.AddDays(1);
        }

        return targetDateTimeOffset;
    }

    private async Task<Response<BackgroundTaskResult>> CheckExpiredGroups()
    {
        logger.LogInformation("Начало проверки истечения групп...");

        var result = new BackgroundTaskResult();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        // Используем UTC для сравнения с PostgreSQL
        var utcNow = DateTimeOffset.UtcNow;
        var expiredGroups = await context.Groups
            .Where(g => g.Status == ActiveStatus.Active && !g.IsDeleted)
            .Where(g => g.EndDate <= utcNow)
            .ToListAsync();

        if (!expiredGroups.Any())
        {
            logger.LogInformation("Просроченных групп не найдено");
            result.Messages.Add("Гурӯҳҳои мӯҳлаташон гузашта ёфт нашуд (Просроченных групп не найдено)");
            return new Response<BackgroundTaskResult>(result) { Message = "Гурӯҳҳои мӯҳлаташон гузашта ёфт нашуд (Просроченных групп не найдено)" };
        }

        logger.LogInformation($"Найдено {expiredGroups.Count} просроченных групп");
        foreach (var group in expiredGroups)
        {
            try
            {
                group.Status = ActiveStatus.Completed;
                group.UpdatedAt = DateTimeOffset.UtcNow;
                result.SuccessCount++;
                result.Messages.Add($"Group {group.Id} marked completed");
                logger.LogInformation($"Группа {group.Id} помечена как завершённая (истекла {group.EndDate:yyyy-MM-dd})");
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.FailedItems.Add(group.Id.ToString());
                result.Messages.Add($"Failed to update group {group.Id}: {ex.Message}");
                logger.LogError(ex, "Failed to update group {groupId}", group.Id);
            }
        }

    await context.SaveChangesAsync();
    logger.LogInformation($"Проверка истечения групп завершена: {result}");
    var message = $"Тағйирот: {result.SuccessCount} гурӯҳҳо муваффақ, {result.FailedCount} ноком (Изменения: {result.SuccessCount} успешно, {result.FailedCount} неуспешно).";
    return new Response<BackgroundTaskResult>(result) { Message = message };
    }
}
