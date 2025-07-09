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
            logger.LogInformation("Checking group expiration status at {time}", localNow);
            await CheckExpiredGroups();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while checking group expiration: {message}", ex.Message);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GroupExpirationService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var nextRunTime = CalculateNextRunTime(now);
                var delay = nextRunTime - now;

                logger.LogInformation($"Next group expiration check scheduled at {nextRunTime.ToDushanbeTime()} (in {delay.TotalHours:F1} hours)");
                await Task.Delay(delay, stoppingToken);
                await CheckExpiredGroups();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while checking group expiration");
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
        logger.LogInformation("Starting group expiration check...");

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
            logger.LogInformation("No expired groups found");
            return;
        }

        logger.LogInformation($"Found {expiredGroups.Count} expired groups");
        foreach (var group in expiredGroups)
        {
            group.Status = ActiveStatus.Inactive;
            group.UpdatedAt = DateTimeOffset.UtcNow;
            logger.LogInformation($"Group {group.Id} marked as inactive (expired on {group.EndDate:yyyy-MM-dd})");
        }

        await context.SaveChangesAsync();
        logger.LogInformation($"Successfully updated {expiredGroups.Count} expired groups");
    }
}
