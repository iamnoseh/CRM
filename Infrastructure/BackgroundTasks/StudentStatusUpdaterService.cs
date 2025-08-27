using Domain.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.DTOs.EmailDTOs;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Services.EmailService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit.Text;

namespace Infrastructure.BackgroundTasks
{
    public class StudentStatusUpdaterService : BackgroundService
    {
        private readonly ILogger<StudentStatusUpdaterService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public StudentStatusUpdaterService(ILogger<StudentStatusUpdaterService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public Task<Response<BackgroundTaskResult>> RunOnceAsync() => UpdateStudentStatusesAsync();

        // Compatibility helper used by Hangfire wiring
        public Task Run()
        {
            return Task.Run(async () => await RunOnceAsync());
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StudentStatusUpdaterService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTimeOffset.UtcNow;
                    var nextRunTime = CalculateNextRunTime(now);
                    var delay = nextRunTime - now;

                    _logger.LogInformation("Next student status update scheduled at {time}", nextRunTime.ToDushanbeTime());
                    await Task.Delay(delay, stoppingToken);
                    await UpdateStudentStatusesAsync();
                }
                catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // shutting down
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in StudentStatusUpdaterService loop");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTimeUtc)
        {
            var local = currentTimeUtc.ToDushanbeTime();
            var targetTime = new TimeSpan(0, 10, 0); // 00:10 Dushanbe
            var candidateLocal = local.Date.Add(targetTime);
            var candidateOffset = new DateTimeOffset(candidateLocal, local.Offset);
            if (local >= candidateOffset)
                candidateOffset = candidateOffset.AddDays(1);
            return candidateOffset.ToUniversalTime();
        }

        private async Task<Response<BackgroundTaskResult>> UpdateStudentStatusesAsync()
        {
            _logger.LogInformation("Start updating student statuses...");

            var result = new BackgroundTaskResult();

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            var emailService = scope.ServiceProvider.GetService<IEmailService>();

            var utcNow = DateTime.UtcNow;

            var studentsToUpdate = await db.Students
                .Where(s => s.ActiveStatus == ActiveStatus.Active && !s.IsDeleted)
                .Where(s => s.NextPaymentDueDate != null && s.NextPaymentDueDate <= utcNow)
                .ToListAsync();

            if (!studentsToUpdate.Any())
            {
                _logger.LogInformation("No students found for update");
                result.Messages.Add("Ягон донишҷӯ барои навсозӣ нест (Нет студентов для обновления)");
                return new Response<BackgroundTaskResult>(result) { Message = result.Messages.Last() };
            }

            _logger.LogInformation("Found {count} students to update", studentsToUpdate.Count);

            foreach (var student in studentsToUpdate)
            {
                try
                {
                    student.PaymentStatus = PaymentStatus.Pending;
                    student.UpdatedAt = DateTimeOffset.UtcNow;
                    result.SuccessCount++;
                    result.Messages.Add($"Student #{student.FullName} marked pending");

                    _logger.LogInformation("Student {FullName} marked pending (due: {due})", student.FullName, student.NextPaymentDueDate);

                    if (emailService != null && !string.IsNullOrWhiteSpace(student.Email))
                    {
                        try
                        {
                            var subject = "Ёдраскуни: мӯҳлати пардохт гузаштааст";
                            var content = $"Салом {student.FullName},\n\nМӯҳлати пардохт ({student.NextPaymentDueDate:yyyy-MM-dd}) гузаштааст. Лутфан пардохтро анҷом диҳед.\n\nБо эҳтиром.";
                            var emailMessage = new EmailMessageDto(new[] { student.Email }, subject, content);
                            await emailService.SendEmail(emailMessage, TextFormat.Plain);
                            result.Messages.Add($"Почтаи электронӣ ба {student.Email} фиристод шуд");
                        }
                        catch (Exception ex)
                        {
                            result.FailedCount++;
                            result.FailedItems.Add(student.Id.ToString());
                            result.Messages.Add($"Failed to send email to {student.Email}: {ex.Message}");
                            _logger.LogWarning(ex, "Failed to send email to student {id}", student.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedItems.Add(student.Id.ToString());
                    result.Messages.Add($"Failed to update student {student.FullName}: {ex.Message}");
                    _logger.LogError(ex, "Failed to update student {FullName}", student.FullName);
                }
            }

            await db.SaveChangesAsync();

            var message = $"Навсозӣ анҷом ёфт: {result.SuccessCount} муваффақ, {result.FailedCount} ноком (Обновлено: {result.SuccessCount} успешно, {result.FailedCount} неуспешно).";
            _logger.LogInformation("Update finished: {msg}", message);
            return new Response<BackgroundTaskResult>(result) { Message = message };
        }
    }
}
