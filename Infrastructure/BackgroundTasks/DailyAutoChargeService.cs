using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundTasks;

public class DailyAutoChargeService(
    ILogger<DailyAutoChargeService> logger,
    IServiceProvider serviceProvider,
    IStudentAccountService studentAccountService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("DailyAutoChargeService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nowUtc = DateTimeOffset.UtcNow;
                var nextRun = CalculateNextRunTime(nowUtc);
                var delay = nextRun - nowUtc;
                logger.LogInformation("Next daily auto-charge scheduled at {time}", nextRun.ToDushanbeTime());
                await Task.Delay(delay, stoppingToken);
                await RunOnceAsync();
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in DailyAutoChargeService loop");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private static DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
    {
        var local = currentTime.ToDushanbeTime();
        var target = new TimeSpan(8, 0, 0); // 08:00 Dushanbe
        var candidate = new DateTimeOffset(local.Date.Add(target), local.Offset);
        if (local >= candidate)
            candidate = candidate.AddDays(1);
        return candidate;
    }

    // Compatibility method for Hangfire wiring
    public Task Run()
    {
        return Task.Run(async () => await RunOnceAsync());
    }

    public async Task RunOnceAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var month = now.Month;
            var year = now.Year;

            // Use per-group charge to ensure per-student status recalculation is executed
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.DataContext>();

            var activeLinks = db.StudentGroups
                .Where(sg => sg.IsActive && !sg.IsDeleted)
                .Select(sg => new { sg.StudentId, sg.GroupId })
                .Distinct()
                .ToList();

            var total = 0;
            foreach (var link in activeLinks)
            {
                try
                {
                    var resp = await studentAccountService.ChargeForGroupAsync(link.StudentId, link.GroupId, month, year);
                    if (resp.StatusCode >= 200 && resp.StatusCode < 300) total++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Charge failed for StudentId={StudentId} GroupId={GroupId}", link.StudentId, link.GroupId);
                }
            }

            logger.LogInformation("DailyAutoChargeService finished: processed {count} active student-group links", total);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DailyAutoChargeService RunOnceAsync error");
            throw;
        }
    }
}


