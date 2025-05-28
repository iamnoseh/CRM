using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundTasks;

/// <summary>
/// Фоновая служба для обновления статусов студентов в завершенных группах
/// </summary>
public class StudentStatusUpdaterService(
    ILogger<StudentStatusUpdaterService> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    /// <summary>
    /// Публичный метод для запуска из Hangfire
    /// </summary>
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
                // Получаем текущее время
                var now = DateTimeOffset.UtcNow;
                
                // Вычисляем время до следующего запуска (00:10) - через 3 минуты после проверки окончания групп
                var nextRunTime = CalculateNextRunTime(now);
                var delay = nextRunTime - now;
                
                logger.LogInformation($"Next student status update scheduled at {nextRunTime} (in {delay.TotalHours:F1} hours)");
                
                // Ждем до следующего запланированного запуска
                await Task.Delay(delay, stoppingToken);
                
                // Обновляем статусы студентов в завершенных группах
                await UpdateStudentStatuses();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while updating student statuses");
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
    /// Вычисляет время следующего запуска (00:10)
    /// </summary>
    private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
    {
        // Запускаем проверку каждый день в 00:10
        var today = currentTime.Date;
        var targetTime = new TimeSpan(0, 10, 0); // 00:10
        
        var targetDateTime = today.Add(targetTime);
        var targetDateTimeOffset = new DateTimeOffset(targetDateTime, currentTime.Offset);
        
        if (currentTime >= targetDateTimeOffset)
        {
            // Если текущее время уже после 00:10, запускаем завтра
            targetDateTimeOffset = targetDateTimeOffset.AddDays(1);
        }
        
        return targetDateTimeOffset;
    }

    /// <summary>
    /// Обновляет статусы студентов в завершенных группах
    /// </summary>
    private async Task UpdateStudentStatuses()
    {
        // Используем скоуп для получения необходимых сервисов
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        try
        {
            // Получаем все группы со статусом "Завершено"
            var completedGroups = await context.Groups
                .Where(g => g.Status == ActiveStatus.Completed && !g.IsDeleted)
                .ToListAsync();
            
            logger.LogInformation($"Found {completedGroups.Count} completed groups for student status update");
            
            int totalUpdated = 0;
            
            foreach (var group in completedGroups)
            {
                // Получаем все связи студента с группой, которые все еще активны
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
                
                // Обновляем статусы для всех студентов в этой группе
                foreach (var studentGroup in activeStudentGroups)
                {
                    // Отмечаем студента как завершившего обучение в этой группе
                    studentGroup.IsActive = false;
                    studentGroup.UpdatedAt = DateTimeOffset.UtcNow;
                    
                    // Добавляем комментарий о завершении обучения
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
            
            // Сохраняем изменения
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
