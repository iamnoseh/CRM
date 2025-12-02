using Hangfire;
using Hangfire.Storage;
using Hangfire.Common;
using Infrastructure.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class HangfireBackgroundTaskService(
    ILogger<HangfireBackgroundTaskService> logger,
    IRecurringJobManager recurringJobManager,
    GroupExpirationService groupExpirationService,
    WeeklyJournalSchedulerService weeklyJournalSchedulerService,
    MonthlyFinanceAggregatorService monthlyFinanceAggregatorService,
    DailyAutoChargeService dailyAutoChargeService)
{
    public void StartAllBackgroundTasks()
    {
        try
        {
            recurringJobManager.AddOrUpdate(
                "group-expiration-check",
                () => groupExpirationService.Run(),
                Cron.Daily(0, 0));
          recurringJobManager.AddOrUpdate(
                "weekly-journal-schedule",
                () => weeklyJournalSchedulerService.ProcessActiveGroupsAsync(CancellationToken.None),
                Cron.Daily(0, 30));
            recurringJobManager.AddOrUpdate(
                "daily-auto-charge",
                () => dailyAutoChargeService.Run(),
                Cron.Daily(3, 0));

            recurringJobManager.AddOrUpdate(
                "monthly-finance-aggregation",
                () => monthlyFinanceAggregatorService.RunAsync(CancellationToken.None),
                "5 0 1 * *");

            logger.LogInformation("Все background tasks успешно запущены как Hangfire recurring jobs");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при запуске background tasks");
            throw;
        }
    }

    public void StopAllBackgroundTasks()
    {
        try
        {
            recurringJobManager.RemoveIfExists("group-expiration-check");
            recurringJobManager.RemoveIfExists("weekly-journal-schedule");
            recurringJobManager.RemoveIfExists("monthly-finance-aggregation");
            recurringJobManager.RemoveIfExists("daily-auto-charge");

            logger.LogInformation("Все background tasks успешно остановлены");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при остановке background tasks");
            throw;
        }
    }

    public void TriggerBackgroundTask(string taskName)
    {
        try
        {
            switch (taskName.ToLower())
            {
                case "group-expiration":
                    recurringJobManager.Trigger("group-expiration-check");
                    logger.LogInformation("Background task 'group-expiration' запущен немедленно");
                    break;
                case "weekly-journal":
                    recurringJobManager.Trigger("weekly-journal-schedule");
                    logger.LogInformation("Background task 'weekly-journal' запущен немедленно");
                    break;
                case "monthly-finance":
                    recurringJobManager.Trigger("monthly-finance-aggregation");
                    logger.LogInformation("Background task 'monthly-finance' запущен немедленно");
                    break;
                case "daily-auto-charge":
                    recurringJobManager.Trigger("daily-auto-charge");
                    logger.LogInformation("Background task 'daily-auto-charge' запущен немедленно");
                    break;
                default:
                    logger.LogWarning("Неизвестный background task: {TaskName}", taskName);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при запуске background task {TaskName}", taskName);
            throw;
        }
    }

    public object GetRecurringJobsStatus()
    {
        using (var connection = JobStorage.Current.GetConnection())
        {
            var recurringJobs = connection.GetRecurringJobs();

            var status = new Dictionary<string, object>();

            foreach (var job in recurringJobs)
            {
                status[job.Id] = new
                {
                    Cron = job.Cron,               
                    NextExecution = job.NextExecution,
                    LastExecution = job.LastExecution,
                    LastJobId = job.LastJobId,
                    LastJobState = job.LastJobState
                };
            }

            return status;
        }
    }
}
