using Domain.Responses;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundTasks;

public class CenterIncomeUpdaterService : BackgroundService
{
    private readonly ILogger<CenterIncomeUpdaterService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public CenterIncomeUpdaterService(
        ILogger<CenterIncomeUpdaterService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }
    
    // Метод для ручного запуска через Hangfire
    public async Task Run()
    {
        _logger.LogInformation("Manual center income update started at: {time}", DateTimeOffset.Now);
        
        using var scope = _scopeFactory.CreateScope();
        var centerService = scope.ServiceProvider.GetRequiredService<CenterService>();
        
        var result = await centerService.CalculateAllCentersIncomeAsync();
        
        if (result.StatusCode == (int)System.Net.HttpStatusCode.OK)
            _logger.LogInformation("Manual center income update completed successfully: {message}", result.Message);
        else
            _logger.LogWarning("Manual center income update completed with warnings: {message}", result.Message);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.Now;
                
                // Запускать в 00:15 каждый день
                var scheduledTime = new TimeSpan(0, 15, 0); // 00:15
                
                if (now.TimeOfDay > scheduledTime)
                {
                    // Если уже позже запланированного времени сегодня, запланировать на завтра
                    var nextRun = now.Date.AddDays(1).Add(scheduledTime);
                    var delay = nextRun - now;
                    _logger.LogInformation("Center income update scheduled for: {time}", nextRun);
                    await Task.Delay(delay, stoppingToken);
                }
                else
                {
                    // Если еще не наступило время сегодня, запланировать на сегодня
                    var nextRun = now.Date.Add(scheduledTime);
                    var delay = nextRun - now;
                    _logger.LogInformation("Center income update scheduled for: {time}", nextRun);
                    await Task.Delay(delay, stoppingToken);
                }
                
                _logger.LogInformation("Center income update started at: {time}", DateTimeOffset.Now);
                
                // Выполнение обновления доходов
                using var scope = _scopeFactory.CreateScope();
                var centerService = scope.ServiceProvider.GetRequiredService<CenterService>();
                
                var result = await centerService.CalculateAllCentersIncomeAsync();
                
                if (result.StatusCode ==(int) System.Net.HttpStatusCode.OK)
                    _logger.LogInformation("Center income update completed successfully: {message}", result.Message);
                else
                    _logger.LogWarning("Center income update completed with warnings: {message}", result.Message);
                
                // Ждем до следующего дня
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating center incomes");
                
                // В случае ошибки пробуем снова через час
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
