using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.BackgroundTasks;

/// <summary>
/// Расширения для регистрации фоновых служб
/// </summary>
public static class BackgroundServiceExtensions
{
    /// <summary>
    /// Добавляет все необходимые фоновые службы в коллекцию сервисов
    /// </summary>
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        // Регистрируем сервис ежедневного создания уроков (запускается каждый день в 00:01)
        services.AddHostedService<DailyLessonCreatorService>();
        
        
        // Регистрируем сервис для отслеживания сроков групп и их деактивации (запускается каждый день в 00:07)
        services.AddHostedService<GroupExpirationService>();
        
        // Регистрируем сервис для обновления статусов студентов в завершенных группах (запускается каждый день в 00:10)
        services.AddHostedService<StudentStatusUpdaterService>();
        
        return services;
    }
}
