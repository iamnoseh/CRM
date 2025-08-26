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
    private const int MaxRetryAttempts = 3;
    private const int RetryDelayMinutes = 5;

    public async Task Run()
    {
        try
        {
            var localNow = DateTimeOffset.UtcNow.ToDushanbeTime();
            logger.LogInformation("Обновление статусов студентов в {time}", localNow);
            await UpdateStudentStatuses();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating student statuses: {message}", ex.Message);
            throw; // Re-throw to allow proper error handling
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Служба обновления статусов студентов запущена");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var nextRunTime = CalculateNextRunTime(now);
                var delay = nextRunTime - now;

                logger.LogInformation($"Следующее обновление статусов студентов запланировано на {nextRunTime.ToDushanbeTime()} (через {delay.TotalHours:F1} часов)");
                await Task.Delay(delay, stoppingToken);
                await UpdateStudentStatuses();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обновлении статусов студентов");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
    
    private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
    {
        var localTime = currentTime.ToDushanbeTime();
        var targetTime = new TimeSpan(0, 10, 0);
        
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
        logger.LogInformation("Начало обновления статусов студентов...");

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var emailService = scope.ServiceProvider.GetService<Services.EmailService.IEmailService>();

        var localNow = DateTimeConfig.NowDushanbe();


        var studentsToUpdate = await context.Students
            .Where(s => s.ActiveStatus == ActiveStatus.Active && !s.IsDeleted)
            .Where(s => s.NextPaymentDueDate != null && s.NextPaymentDueDate <= localNow)
            .ToListAsync();

        if (!studentsToUpdate.Any())
        {
            logger.LogInformation("Нет студентов, требующих обновления статуса");
            return;
        }

        logger.LogInformation($"Найдено {studentsToUpdate.Count} студентов, требующих обновления статуса");

        foreach (var student in studentsToUpdate)
        {
            student.PaymentStatus = PaymentStatus.Pending;
            student.UpdatedAt = DateTimeOffset.UtcNow;
            logger.LogInformation($"Студент {student.Id} помечен как неактивный, статус оплаты — Ожидание (срок оплаты {student.NextPaymentDueDate:yyyy-MM-dd})");

            if (emailService != null && !string.IsNullOrWhiteSpace(student.Email))
            {
                try
                {
                    var subject = "Напоминание: Срок оплаты истёк";
                    var content = $"Здравствуйте, {student.FullName}!\n\nСрок оплаты истёк. Пожалуйста, выполните оплату как можно скорее.\n\nСпасибо.";
                    var message = new Domain.DTOs.EmailDTOs.EmailMessageDto(new[] { student.Email }, subject, content);
                    await emailService.SendEmail(message, MimeKit.Text.TextFormat.Plain);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send payment reminder email to student {studentId}", student.Id);
                }
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation($"Успешно обновлены статусы {studentsToUpdate.Count} студентов");
    }
}
