using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Helpers;
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
        try
        {
            var localNow = DateTimeOffset.UtcNow.ToDushanbeTime();
            logger.LogInformation("Обновление опыта преподавателей в {time}", localNow);
            await UpdateMentorsExperience();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении опыта преподавателей: {message}", ex.Message);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Служба обновления опыта преподавателей запущена");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var nextRunTime = CalculateNextRunTime(now);
                var delay = nextRunTime - now;

                logger.LogInformation($"Следующее обновление опыта преподавателей запланировано на {nextRunTime.ToDushanbeTime()} (через {delay.TotalHours:F1} часов)");
                await Task.Delay(delay, stoppingToken);
                await UpdateMentorsExperience();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обновлении опыта преподавателей");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
    {
        var localTime = currentTime.ToDushanbeTime();
        var targetTime = new TimeSpan(0, 15, 0); // 00:15
        var nextYear = localTime.Year;
        var targetDate = new DateTime(nextYear, 1, 1);
        var targetRunTime = targetDate.Add(targetTime);
        var targetDateTimeOffset = new DateTimeOffset(targetRunTime, localTime.Offset);

        if (localTime >= targetDateTimeOffset)
        {
            targetDateTimeOffset = targetDateTimeOffset.AddYears(1);
        }

        return targetDateTimeOffset;
    }
    
    /// <summary>
    /// Вычисляет опыт на основе даты начала работы в центре
    /// </summary>
    private int CalculateExperienceYears(DateTime joinDate)
    {
        var now = DateTimeConfig.NowDushanbe();
        var years = now.Year - joinDate.Year;
        if (joinDate.Date > now.AddYears(-years)) years--;
        return years;
    }

    private async Task UpdateMentorsExperience()
    {
        logger.LogInformation("Начало обновления опыта преподавателей...");

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var mentors = await context.Mentors
            .Where(m => !m.IsDeleted)
            .ToListAsync();

        if (!mentors.Any())
        {
            logger.LogInformation("Активные преподаватели не найдены");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        logger.LogInformation($"Найдено {mentors.Count} преподавателей для обновления");

        foreach (var mentor in mentors)
        {
            // Use CreatedAt as join date
            var experienceYears = CalculateExperienceYears(mentor.CreatedAt.DateTime);
            mentor.Experience = experienceYears;
            mentor.UpdatedAt = now;
            logger.LogInformation($"Обновлен опыт преподавателя {mentor.Id}: {experienceYears} лет (дата начала: {mentor.CreatedAt:yyyy-MM-dd})");
        }

        await context.SaveChangesAsync();
        logger.LogInformation($"Успешно обновлен опыт {mentors.Count} преподавателей");
    }
}
