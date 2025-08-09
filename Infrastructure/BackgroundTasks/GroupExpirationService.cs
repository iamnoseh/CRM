using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            await CheckExpiredGroups();
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

                logger.LogInformation($"Следующая проверка истечения групп запланирована на {nextRunTime.ToDushanbeTime()} (через {delay.TotalHours:F1} часов)");
                await Task.Delay(delay, stoppingToken);
                await CheckExpiredGroups();
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

    private async Task CheckExpiredGroups()
    {
        logger.LogInformation("Начало проверки истечения групп...");

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
            return;
        }

        logger.LogInformation($"Найдено {expiredGroups.Count} просроченных групп");
        foreach (var group in expiredGroups)
        {
            group.Status = ActiveStatus.Completed;
            group.UpdatedAt = DateTimeOffset.UtcNow;
            logger.LogInformation($"Группа {group.Id} помечена как завершённая (истекла {group.EndDate:yyyy-MM-dd})");
        }

        await context.SaveChangesAsync();
        logger.LogInformation($"Успешно обновлено {expiredGroups.Count} просроченных групп");
    }
}
