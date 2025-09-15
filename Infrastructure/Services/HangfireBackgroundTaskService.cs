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
    StudentStatusUpdaterService studentStatusUpdaterService,
    WeeklyJournalSchedulerService weeklyJournalSchedulerService)
{
    public void StartAllBackgroundTasks()
    {
        try
        {
            recurringJobManager.AddOrUpdate(
                "group-expiration-check",
                () => groupExpirationService.Run(),
                Cron.Hourly);

            recurringJobManager.AddOrUpdate(
                "student-status-update",
                () => studentStatusUpdaterService.Run(),
                Cron.Daily(6, 0));

          recurringJobManager.AddOrUpdate(
                "weekly-journal-schedule",
                () => weeklyJournalSchedulerService.ProcessActiveGroupsAsync(CancellationToken.None),
                Cron.Daily(7, 0));

            // Monthly payroll generation on the 1st day at 06:10 UTC for previous month per center can be triggered via FinanceController endpoint or separate job if center list known.

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
            recurringJobManager.RemoveIfExists("student-status-update");
            recurringJobManager.RemoveIfExists("weekly-journal-schedule");

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
                case "student-status":
                    recurringJobManager.Trigger("student-status-update");
                    logger.LogInformation("Background task 'student-status' запущен немедленно");
                    break;
                case "weekly-journal":
                    recurringJobManager.Trigger("weekly-journal-schedule");
                    logger.LogInformation("Background task 'weekly-journal' запущен немедленно");
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
