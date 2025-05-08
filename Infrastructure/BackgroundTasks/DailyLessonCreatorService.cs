using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundTasks;

/// <summary>
/// Фоновая служба для автоматического создания уроков каждый день в 00:01 с понедельника по пятницу
/// </summary>
public class DailyLessonCreatorService(
    ILogger<DailyLessonCreatorService> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Daily Lesson Creator Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Получаем текущее время
                var now = DateTimeOffset.UtcNow;
                
                // Вычисляем время до следующего запуска (00:01)
                var nextRunTime = CalculateNextRunTime(now);
                var delay = nextRunTime - now;
                
                logger.LogInformation($"Next lesson creation scheduled at {nextRunTime} (in {delay.TotalHours:F1} hours)");
                
                // Ждем до следующего запланированного запуска
                await Task.Delay(delay, stoppingToken);
                
                // Проверяем день недели (с понедельника по пятницу)
                var dayOfWeek = DateTimeOffset.UtcNow.DayOfWeek;
                if (dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Friday)
                {
                    await CreateLessonsForToday();
                }
                else
                {
                    logger.LogInformation($"Skipping lesson creation on {dayOfWeek}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while creating daily lessons");
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

    /// <summary>
    /// Вычисляет время следующего запуска (00:01)
    /// </summary>
    private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
    {
        // Если текущее время до 00:01, то запускаем сегодня в 00:01
        // Иначе запускаем завтра в 00:01
        var today = currentTime.Date;
        var targetTime = new TimeSpan(0, 1, 0); // 00:01
        
        var targetDateTime = today.Add(targetTime);
        var targetDateTimeOffset = new DateTimeOffset(targetDateTime, currentTime.Offset);
        
        if (currentTime >= targetDateTimeOffset)
        {
            // Если текущее время уже после 00:01, запускаем завтра
            targetDateTimeOffset = targetDateTimeOffset.AddDays(1);
        }
        
        return targetDateTimeOffset;
    }

    /// <summary>
    /// Создает уроки для всех активных групп на сегодняшний день
    /// </summary>
    private async Task CreateLessonsForToday()
    {
        // Используем скоуп для получения необходимых сервисов
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        var today = DateTimeOffset.UtcNow.Date;
        var dayOfWeek = today.DayOfWeek;
        var dayOfWeekIndex = ((int)dayOfWeek) - 1; // 0 для понедельника, 4 для пятницы
        
        logger.LogInformation($"Creating lessons for {dayOfWeek} (index {dayOfWeekIndex})");
        
        try
        {
            // Получаем все активные группы
            var activeGroups = await context.Groups
                .Where(g => g.Started && 
                          today >= g.StartDate.Date && 
                          today <= g.EndDate.Date && 
                          !g.IsDeleted)
                .ToListAsync();
            
            logger.LogInformation($"Found {activeGroups.Count} active groups for lesson creation");
            
            // Определяем текущую неделю обучения для каждой группы
            foreach (var group in activeGroups)
            {
                // Вычисляем индекс текущей недели относительно даты начала группы
                var weeksSinceStart = (int)Math.Floor((today - group.StartDate.Date).TotalDays / 7);
                var weekIndex = weeksSinceStart % 4 + 1; // Используем 4-недельный цикл (1-4)
                
                logger.LogInformation($"Creating lesson for group {group.Id}, week {weekIndex}, day {dayOfWeekIndex}");
                
                // Проверяем, нет ли уже урока на этот день для этой группы
                var existingLesson = await context.Lessons
                    .AnyAsync(l => l.GroupId == group.Id && 
                                  l.WeekIndex == weekIndex && 
                                  l.DayOfWeekIndex == dayOfWeekIndex && 
                                  l.StartTime.Date == today &&
                                  !l.IsDeleted);
                
                if (existingLesson)
                {
                    logger.LogInformation($"Lesson already exists for group {group.Id} on {today.ToString("yyyy-MM-dd")}");
                    continue;
                }
                
                // Создаем урок на текущий день
                var lesson = new Lesson
                {
                    GroupId = group.Id,
                    WeekIndex = weekIndex,
                    DayOfWeekIndex = dayOfWeekIndex,
                    StartTime = new DateTimeOffset(today.Year, today.Month, today.Day, 
                                                 9, 0, 0, 
                                                 TimeSpan.Zero), 
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                
                await context.Lessons.AddAsync(lesson);
                logger.LogInformation($"Created new lesson for group {group.Id} on {today.ToString("yyyy-MM-dd")}");
            }
           
            var savedCount = await context.SaveChangesAsync();
            logger.LogInformation($"Successfully created {savedCount} lessons");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating lessons for today");
            throw;
        }
    }
}
