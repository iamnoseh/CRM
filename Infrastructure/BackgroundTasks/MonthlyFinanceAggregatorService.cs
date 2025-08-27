using Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundTasks;

public class MonthlyFinanceAggregatorService(ILogger<MonthlyFinanceAggregatorService> logger, IServiceProvider serviceProvider)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Служба ежемесячной финансовой агрегации запущена");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var next = CalculateNextRunTime(now);
                var delay = next - now;
                logger.LogInformation("Следующий запуск финансовой агрегации: {time}", next);
                await Task.Delay(delay, stoppingToken);
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка в цикле MonthlyFinanceAggregatorService");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private static DateTimeOffset CalculateNextRunTime(DateTimeOffset current)
    {
        var firstOfNextMonth = new DateTimeOffset(new DateTime(current.Year, current.Month, 1, 0, 5, 0, DateTimeKind.Utc)).AddMonths(1);
        return firstOfNextMonth;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();
        var centersService = scope.ServiceProvider.GetRequiredService<IGroupService>();
        try
        {
            // TODO: Реализовать получение списка центров и кеширование сводок при необходимости
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка выполнения MonthlyFinanceAggregatorService");
        }
    }
}


