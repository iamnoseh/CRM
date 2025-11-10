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
            // Current local date in Dushanbe
            var nowUtc = DateTimeOffset.UtcNow;
            var nowLocal = nowUtc.ToDushanbeTime();
            var month = nowLocal.Month;
            var year = nowLocal.Year;
            var today = nowLocal.Day;

            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.DataContext>();

            // Pull active links for ACTIVE, non-completed groups only
            var nowBoundaryUtc = DateTimeOffset.UtcNow;
            var activeLinks = db.StudentGroups
                .Where(sg => sg.IsActive && !sg.IsDeleted)
                .Join(db.Groups.Where(g => !g.IsDeleted && g.Status == Domain.Enums.ActiveStatus.Active && g.EndDate > nowBoundaryUtc),
                      sg => sg.GroupId,
                      g => g.Id,
                      (sg, g) => new { sg.StudentId, sg.GroupId, sg.JoinDate })
                .ToList();

            var total = 0;
            var daysInMonth = DateTime.DaysInMonth(year, month);
            foreach (var link in activeLinks)
            {
                try
                {
                    // Determine student's due day based on join date (Dushanbe local)
                    var joinLocal = new DateTimeOffset(DateTime.SpecifyKind(link.JoinDate, DateTimeKind.Utc)).ToDushanbeTime();
                    var joinDay = joinLocal.Day;
                    var dueDayThisMonth = Math.Min(joinDay, daysInMonth);

                    // First charge starts next month after join
                    var isSameMonthAsJoin = (joinLocal.Month == month && joinLocal.Year == year);
                    if (isSameMonthAsJoin)
                        continue;

                    if (today != dueDayThisMonth)
                        continue;

                    var resp = await studentAccountService.ChargeForGroupAsync(link.StudentId, link.GroupId, month, year);
                    if (resp.StatusCode >= 200 && resp.StatusCode < 300) total++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Charge failed for StudentId={StudentId} GroupId={GroupId}", link.StudentId, link.GroupId);
                }
            }

            logger.LogInformation("DailyAutoChargeService finished: processed {count} due student-group links for {month}.{year}", total, month, year);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DailyAutoChargeService RunOnceAsync error");
            throw;
        }
    }
}


