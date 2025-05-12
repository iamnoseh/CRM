using Domain.Responses;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundTasks;

public class CenterIncomeUpdaterService(
    ILogger<CenterIncomeUpdaterService> logger,
    IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    public async Task Run()
    {
        logger.LogInformation("Manual center income update started at: {time}", DateTimeOffset.Now);
        
        using var scope = scopeFactory.CreateScope();
        var centerService = scope.ServiceProvider.GetRequiredService<CenterService>();
        
        var result = await centerService.CalculateAllCentersIncomeAsync();
        
        if (result.StatusCode == (int)System.Net.HttpStatusCode.OK)
            logger.LogInformation("Manual center income update completed successfully: {message}", result.Message);
        else
            logger.LogWarning("Manual center income update completed with warnings: {message}", result.Message);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.Now;
                var scheduledTime = new TimeSpan(0, 15, 0); // 00:15
                
                if (now.TimeOfDay > scheduledTime)
                {
                    var nextRun = now.Date.AddDays(1).Add(scheduledTime);
                    var delay = nextRun - now;
                    logger.LogInformation("Center income update scheduled for: {time}", nextRun);
                    await Task.Delay(delay, stoppingToken);
                }
                else
                {
                    var nextRun = now.Date.Add(scheduledTime);
                    var delay = nextRun - now;
                    logger.LogInformation("Center income update scheduled for: {time}", nextRun);
                    await Task.Delay(delay, stoppingToken);
                }
                
                logger.LogInformation("Center income update started at: {time}", DateTimeOffset.Now);
                using var scope = scopeFactory.CreateScope();
                var centerService = scope.ServiceProvider.GetRequiredService<CenterService>();
                
                var result = await centerService.CalculateAllCentersIncomeAsync();
                
                if (result.StatusCode ==(int) System.Net.HttpStatusCode.OK)
                    logger.LogInformation("Center income update completed successfully: {message}", result.Message);
                else
                    logger.LogWarning("Center income update completed with warnings: {message}", result.Message);
                
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while updating center incomes");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
