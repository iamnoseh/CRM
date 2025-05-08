using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Group Expiration Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var nextRunTime = CalculateNextRunTime(now);
                var delay = nextRunTime - now;
                
                logger.LogInformation($"Next group expiration check scheduled at {nextRunTime} (in {delay.TotalHours:F1} hours)");
                await Task.Delay(delay, stoppingToken);
                await CheckExpiredGroups();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while checking expired groups");
            }
            
            // Ждем некоторое время перед следующей итерацией цикла
            // Это предотвращает слишком частые проверки, если произошла ошибка
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Подавляем исключение TaskCanceledException при остановке сервиса
                break;
            }
        }
    }

    private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
    {
       
        var today = currentTime.Date;
        var targetTime = new TimeSpan(0, 7, 0); // 00:07
        
        var targetDateTime = today.Add(targetTime);
        var targetDateTimeOffset = new DateTimeOffset(targetDateTime, currentTime.Offset);
        
        if (currentTime >= targetDateTimeOffset)
        {
            targetDateTimeOffset = targetDateTimeOffset.AddDays(1);
        }
        
        return targetDateTimeOffset;
    }
    
    private async Task CheckExpiredGroups()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        var today = DateTimeOffset.UtcNow.Date;
        
        try
        {
            var expiredGroups = await context.Groups
                .Where(g => g.Status == ActiveStatus.Active && 
                          g.Started && 
                          g.EndDate.Date < today && 
                          !g.IsDeleted)
                .ToListAsync();
            
            logger.LogInformation($"Found {expiredGroups.Count} expired groups to deactivate");
            
            foreach (var group in expiredGroups)
            {
                group.Status = ActiveStatus.Completed;
                group.UpdatedAt = DateTimeOffset.UtcNow;
                
                logger.LogInformation($"Group {group.Id} ({group.Name}) has been marked as completed. End date: {group.EndDate.Date.ToString("yyyy-MM-dd")}");
                
                var mentorGroups = await context.MentorGroups
                    .Where(mg => mg.GroupId == group.Id && 
                               (bool)mg.IsActive && 
                               !mg.IsDeleted)
                    .ToListAsync();
                
                foreach (var mentorGroup in mentorGroups)
                {
                    mentorGroup.IsActive = false;
                    mentorGroup.UpdatedAt = DateTimeOffset.UtcNow;
                }
                
                logger.LogInformation($"Deactivated {mentorGroups.Count} mentor-group relationships for group {group.Id}");
            }
            
            await context.SaveChangesAsync();
            logger.LogInformation($"Successfully updated {expiredGroups.Count} expired groups");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking expired groups");
            throw;
        }
    }
}
