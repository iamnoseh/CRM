using Domain.Enums;
using Domain.DTOs.EmailDTOs;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
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
        // ИН BACKGROUND TASK ДИГАР ИСТИФОДА НАМЕШАВАД
        // Ҳамаи логикаи пардохт ва огоҳӣ дар DailyAutoChargeService ҳаст
        _logger.LogInformation("StudentStatusUpdaterService: Хизмат ғайрифаъол карда шуд. Истифодаи DailyAutoChargeService тавсия дода мешавад.");
        
        var result = new BackgroundTaskResult();
        result.Messages.Add("Ин хизмат ғайрифаъол карда шуд. Пардохтҳо тавассути DailyAutoChargeService идора мешаванд.");
        
        return new Response<BackgroundTaskResult>(result) { Message = result.Messages.Last() };
    }
    }
}
