using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.BackgroundTasks;

public static class BackgroundServiceExtensions
{
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        // Keep only essential background tasks
        services.AddHostedService<GroupExpirationService>();
        services.AddHostedService<StudentStatusUpdaterService>();

        return services;
    }
}
