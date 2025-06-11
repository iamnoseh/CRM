using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Helpers;
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
        try
        {
            var localNow = DateTimeOffset.UtcNow.ToDushanbeTime();
            logger.LogInformation("Updating student statuses at {time}", localNow);
            await UpdateStudentStatuses();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating student statuses: {message}", ex.Message);
        }
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

                logger.LogInformation($"Next student status update scheduled at {nextRunTime.ToDushanbeTime()} (in {delay.TotalHours:F1} hours)");
                await Task.Delay(delay, stoppingToken);
                await UpdateStudentStatuses();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while updating student statuses");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
    
    private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
    {
        var localTime = currentTime.ToDushanbeTime();
        var targetTime = new TimeSpan(0, 10, 0); // 00:10 AM
        
        var targetRunTime = localTime.Date.Add(targetTime);
        var targetDateTimeOffset = new DateTimeOffset(targetRunTime, localTime.Offset);
        
        if (localTime >= targetDateTimeOffset)
        {
            targetDateTimeOffset = targetDateTimeOffset.AddDays(1);
        }
        
        return targetDateTimeOffset;
    }
    
    private async Task UpdateStudentStatuses()
    {
        logger.LogInformation("Starting student status update...");

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var localNow = DateTimeConfig.NowDushanbe();

        // Get all active students who need payment update
        var studentsToUpdate = await context.Students
            .Where(s => s.ActiveStatus == ActiveStatus.Active && !s.IsDeleted)
            .Where(s => s.NextPaymentDueDate != null && s.NextPaymentDueDate <= localNow)
            .ToListAsync();

        if (!studentsToUpdate.Any())
        {
            logger.LogInformation("No students need status update");
            return;
        }

        logger.LogInformation($"Found {studentsToUpdate.Count} students needing status update");

        foreach (var student in studentsToUpdate)
        {
            student.ActiveStatus = ActiveStatus.Inactive;
            student.UpdatedAt = DateTimeOffset.UtcNow;
            logger.LogInformation($"Student {student.Id} marked as inactive (payment due on {student.NextPaymentDueDate:yyyy-MM-dd})");
        }

        await context.SaveChangesAsync();
        logger.LogInformation($"Successfully updated {studentsToUpdate.Count} student statuses");
    }
}
