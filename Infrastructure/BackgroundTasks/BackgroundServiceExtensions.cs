// using Microsoft.Extensions.DependencyInjection;
//
// namespace Infrastructure.BackgroundTasks;
//
// public static class BackgroundServiceExtensions
// {
//     public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
//     {
//         services.AddHostedService<DailyLessonCreatorService>();
//         services.AddHostedService<GroupExpirationService>();
//         services.AddHostedService<StudentStatusUpdaterService>();
//         services.AddHostedService<MentorExperienceUpdaterService>();
//
//         return services;
//     }
// }
