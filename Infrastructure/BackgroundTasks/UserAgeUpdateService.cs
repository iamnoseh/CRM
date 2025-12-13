using Infrastructure.Data;
using Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundTasks;

public class UserAgeUpdateService(
    ILogger<UserAgeUpdateService> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("UserAgeUpdateService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nowUtc = DateTimeOffset.UtcNow;
                var nextRun = CalculateNextRunTime(nowUtc);
                var delay = nextRun - nowUtc;
                logger.LogInformation("Next user age update scheduled at {time}", nextRun.ToDushanbeTime());
                await Task.Delay(delay, stoppingToken);
                await RunOnceAsync();
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in UserAgeUpdateService loop");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private static DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
    {
        var local = currentTime.ToDushanbeTime();
        var target = new TimeSpan(2, 0, 0); // 02:00 AM Dushanbe time
        var candidate = new DateTimeOffset(local.Date.Add(target), local.Offset);
        if (local >= candidate)
            candidate = candidate.AddDays(1);
        return candidate;
    }

    public async Task RunOnceAsync()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            var today = DateTimeOffset.UtcNow.ToDushanbeTime().Date;

            // Get all users who have birthday today (month and day match)
            var usersWithBirthdayToday = await db.Users
                .Where(u => !u.IsDeleted && 
                           u.Birthday.Month == today.Month && 
                           u.Birthday.Day == today.Day)
                .ToListAsync();

            var updatedCount = 0;

            foreach (var user in usersWithBirthdayToday)
            {
                try
                {
                    var newAge = CalculateAge(user.Birthday, today);
                    
                    if (user.Age != newAge)
                    {
                        user.Age = newAge;
                        user.UpdatedAt = DateTime.UtcNow;
                        updatedCount++;
                        
                        logger.LogInformation(
                            "Updated age for user {UserId} ({FullName}): {OldAge} → {NewAge}",
                            user.Id, user.FullName, user.Age - 1, newAge);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to update age for user {UserId}", user.Id);
                }
            }

            if (updatedCount > 0)
            {
                await db.SaveChangesAsync();
                logger.LogInformation(
                    "UserAgeUpdateService completed: Updated age for {count} users with birthday today",
                    updatedCount);
            }
            else
            {
                logger.LogInformation("UserAgeUpdateService completed: No users with birthday today");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "UserAgeUpdateService RunOnceAsync error");
            throw;
        }
    }

    private static int CalculateAge(DateTime birthDate, DateTime currentDate)
    {
        var age = currentDate.Year - birthDate.Year;
        
        // Check if birthday hasn't occurred yet this year
        if (currentDate.Month < birthDate.Month || 
            (currentDate.Month == birthDate.Month && currentDate.Day < birthDate.Day))
        {
            age--;
        }
        
        return age;
    }
}
