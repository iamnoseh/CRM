using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin,Manager")]
public class BackgroundTaskController(
    Infrastructure.Services.HangfireBackgroundTaskService hangfireTaskService,
    ILogger<BackgroundTaskController> logger)
    : ControllerBase
{
    [HttpPost("start-all")]
    public IActionResult StartAllBackgroundTasks()
    {
        try
        {
            hangfireTaskService.StartAllBackgroundTasks();
            return Ok(new { message = "Все background tasks успешно запущены" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при запуске background tasks");
            return StatusCode(500, new { error = $"Ошибка при запуске background tasks: {ex.Message}" });
        }
    }

    
    [HttpPost("stop-all")]
    public IActionResult StopAllBackgroundTasks()
    {
        try
        {
            hangfireTaskService.StopAllBackgroundTasks();
            return Ok(new { message = "Все background tasks успешно остановлены" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при остановке background tasks");
            return StatusCode(500, new { error = $"Ошибка при остановке background tasks: {ex.Message}" });
        }
    }

   
    [HttpPost("trigger/{taskName}")] 
    public IActionResult TriggerBackgroundTask(string taskName)
    {
        try
        {
            if (string.IsNullOrEmpty(taskName))
            {
                return BadRequest(new { error = "TaskName обязателен" });
            }

            hangfireTaskService.TriggerBackgroundTask(taskName);
            return Ok(new { message = $"Background task '{taskName}' успешно запущен" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при запуске background task {TaskName}", taskName);
            return StatusCode(500, new { error = $"Ошибка при запуске background task: {ex.Message}" });
        }
    }

    
    [HttpGet("status")]
    
    public IActionResult GetBackgroundTasksStatus()
    {
        try
        {
            var status = hangfireTaskService.GetRecurringJobsStatus();
            return Ok(status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении статуса background tasks");
            return StatusCode(500, new { error = $"Ошибка при получении статуса background tasks: {ex.Message}" });
        }
    }

    
    [HttpGet("info")]
    public IActionResult GetBackgroundTasksInfo()
    {
        var tasks = new[]
        {
            new { 
                Name = "group-expiration", 
                Description = "Проверка истечения групп", 
                Schedule = "Каждый час",
                Service = "GroupExpirationService"
            },
            new { 
                Name = "student-status", 
                Description = "Обновление статуса студентов", 
                Schedule = "Каждый день в 6:00",
                Service = "StudentStatusUpdaterService"
            },
            new { 
                Name = "weekly-journal", 
                Description = "Планирование еженедельных журналов", 
                Schedule = "Каждый день в 7:00",
                Service = "WeeklyJournalSchedulerService"
            }
        };

        return Ok(tasks);
    }
}
