using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundTasks;
public class MentorExperienceUpdaterService(
    ILogger<MentorExperienceUpdaterService> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    public async Task Run()
    {
        logger.LogInformation("Manual run of Mentor Experience Updater Service triggered");
        await UpdateMentorsExperience();
        logger.LogInformation("Manual run of Mentor Experience Updater Service completed");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Mentor Experience Updater Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Обновляем опыт всех активных менторов
                await UpdateMentorsExperience();
                
                // Рассчитываем время следующего обновления (каждые 24 часа)
                var nextRunTime = DateTimeOffset.UtcNow.AddDays(1);
                var delay = nextRunTime - DateTimeOffset.UtcNow;
                
                // Ограничиваем максимальную задержку 24 часами на случай, если что-то пойдет не так
                delay = TimeSpan.FromHours(Math.Min(delay.TotalHours, 24));
                
                logger.LogInformation($"Next mentor experience update scheduled at {nextRunTime:yyyy-MM-dd HH:mm:ss} (in {delay.TotalHours:F1} hours)");
                
                // Используем Task.Delay с проверкой на разумный таймаут
                if (delay > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(delay, stoppingToken);
                    }
                    catch (TaskCanceledException)
                    {
                        // Выходим из цикла, если получили запрос на отмену
                        break;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error occurred while updating mentor experience. Will retry in 1 hour.");
                // В случае ошибки ждем час перед следующей попыткой
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }
    
    private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
    {
        int nextYear = currentTime.Year;
        var targetTime = new TimeSpan(0, 15, 0); // 00:15
        var targetDate = new DateTime(nextYear, 1, 1);
        var targetDateTime = targetDate.Add(targetTime);
        var targetDateTimeOffset = new DateTimeOffset(targetDateTime, currentTime.Offset);
        
        if (currentTime >= targetDateTimeOffset)
        {
            targetDateTimeOffset = targetDateTimeOffset.AddYears(1);
        }
        
        return targetDateTimeOffset;
    }

    /// <summary>
    /// Вычисляет опыт на основе даты регистрации
    /// </summary>
    private int CalculateExperienceYears(DateTime registrationDate)
    {
        var today = DateTime.Today;
        var yearsOfExperience = today.Year - registrationDate.Year;
        if (registrationDate.Date > today.AddYears(-yearsOfExperience))
        {
            yearsOfExperience--;
        }
        
        return yearsOfExperience;
    }
    

    private async Task UpdateMentorsExperience()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        try
        {
            var activeMentors = await context.Mentors
                .Where(m => m.ActiveStatus == ActiveStatus.Active && !m.IsDeleted)
                .ToListAsync();
            
            logger.LogInformation($"Found {activeMentors.Count} active mentors for experience update");
            
            int totalUpdated = 0;
            
            foreach (var mentor in activeMentors)
            {
                var registrationDate = mentor.CreatedAt.DateTime;
                var calculatedExperience = CalculateExperienceYears(registrationDate);
                if (mentor.Experience != calculatedExperience)
                {
                    var previousExperience = mentor.Experience;
                    mentor.Experience = calculatedExperience;
                    mentor.UpdatedAt = DateTime.UtcNow;
                    var user = await context.Users
                        .FirstOrDefaultAsync(u => u.Id == mentor.UserId && !u.IsDeleted);
                    
                    if (user != null)
                    {
                        user.UpdatedAt = DateTime.UtcNow;
                        context.Users.Update(user);
                    }
                    
                    totalUpdated++;
                    
                    logger.LogInformation($"Updated mentor {mentor.Id} ({mentor.FullName}): Experience changed from {previousExperience} to {calculatedExperience} years (registered on {registrationDate:yyyy-MM-dd})");
                }
            }
            if (totalUpdated > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation($"Successfully updated experience for {totalUpdated} mentors");
            }
            else
            {
                logger.LogInformation("No experience updates needed for any mentors");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating mentor experience");
            throw;
        }
    }
}
