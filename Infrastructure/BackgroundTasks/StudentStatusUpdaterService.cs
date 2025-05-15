using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundTasks;

public class StudentStatusUpdaterService(ILogger<StudentStatusUpdaterService> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    public async Task Run()
    {
        logger.LogInformation("Manual run of Student Status Updater Service triggered");
        await UpdateStudentStatuses();
        logger.LogInformation("Manual run of Student Status Updater Service completed");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Student Status Updater Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var nextRunTime = CalculateNextRunTime(now);
                var delay = nextRunTime - now;
                
                logger.LogInformation($"Next student status update scheduled at {nextRunTime} (in {delay.TotalHours:F1} hours)");
                await Task.Delay(delay, stoppingToken);
                await UpdateStudentStatuses();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while updating student statuses");
            }
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
    
    private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
    {
        var today = currentTime.Date;
        var targetTime = new TimeSpan(0, 10, 0); // 00:10
        
        var targetDateTime = today.Add(targetTime);
        var targetDateTimeOffset = new DateTimeOffset(targetDateTime, currentTime.Offset);
        
        if (currentTime >= targetDateTimeOffset)
        {
            targetDateTimeOffset = targetDateTimeOffset.AddDays(1);
        }
        
        return targetDateTimeOffset;
    }
    
    private async Task UpdateStudentStatuses()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        try
        {
            var completedGroups = await context.Groups
                .Where(g => g.Status == ActiveStatus.Completed && !g.IsDeleted)
                .ToListAsync();
            
            logger.LogInformation($"Found {completedGroups.Count} completed groups for student status update");
            
            int totalUpdated = 0;
            
            foreach (var group in completedGroups)
            {
                var activeStudentGroups = await context.StudentGroups
                    .Where(sg => sg.GroupId == group.Id && 
                              (bool)sg.IsActive && 
                              !sg.IsDeleted)
                    .ToListAsync();
                
                if (activeStudentGroups.Count == 0)
                {
                    logger.LogInformation($"No active students found in completed group {group.Id} ({group.Name})");
                    continue;
                }
                
                logger.LogInformation($"Updating status for {activeStudentGroups.Count} students in completed group {group.Id} ({group.Name})");
                
                foreach (var studentGroup in activeStudentGroups)
                {
                    studentGroup.IsActive = false;
                    studentGroup.UpdatedAt = DateTimeOffset.UtcNow;

                    var completionComment = new Comment
                    {
                        Text = $"Студент успешно завершил обучение в группе {group.Name}",
                        GroupId = group.Id,
                        StudentId = studentGroup.StudentId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    
                    await context.Comments.AddAsync(completionComment);
                    totalUpdated++;
                }
            }
            await context.SaveChangesAsync();
            logger.LogInformation($"Successfully updated status for {totalUpdated} students in completed groups");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating student statuses in completed groups");
            throw;
        }
    }
}
