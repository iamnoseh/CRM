using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundTasks;

public static class BackgroundServiceExtensions
{
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<GroupExpirationService>();
        services.AddHostedService<WeeklyJournalSchedulerService>();

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        return services;
    }

    public static IServiceCollection AddBackgroundServicesWithRetry(this IServiceCollection services, Action<BackgroundServiceOptions> configureOptions = null)
    {
        var options = new BackgroundServiceOptions();
        configureOptions?.Invoke(options);

        services.AddHostedService<GroupExpirationService>();
        services.AddHostedService<WeeklyJournalSchedulerService>();

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        return services;
    }
}

public class BackgroundServiceOptions
{
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMinutes { get; set; } = 5;
    public bool EnableDetailedLogging { get; set; } = true;
}
