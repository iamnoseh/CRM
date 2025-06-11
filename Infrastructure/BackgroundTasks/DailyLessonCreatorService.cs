using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundTasks
{
    public class DailyLessonCreatorService(
        ILogger<DailyLessonCreatorService> logger,
        IServiceProvider serviceProvider)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("DailyLessonCreatorService started at {time}", DateTimeOffset.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var utcNow = DateTimeOffset.UtcNow;
                    var localNow = utcNow.ToDushanbeTime();
                    var today = localNow.Date;
                    logger.LogInformation("Рӯзи ҷорӣ: {dayOfWeek} ({today})", localNow.DayOfWeek, today.ToString("yyyy-MM-dd"));

                    await CheckAndCreateLessonsForToday(stoppingToken, today);

                    var nextRun = CalculateNextRunTime(localNow);
                    var delay = nextRun - utcNow;

                    if (delay <= TimeSpan.Zero)
                    {
                        delay = TimeSpan.FromMinutes(1);
                    }

                    logger.LogInformation($"Интизори иҷрои навбатӣ: {delay.TotalHours:F1} соат");
                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Хатогӣ ҳангоми сохтани дарсҳои рӯзона");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentLocalTime)
        {
            var executionTime = new TimeSpan(0, 1, 0); // 00:01 AM
            var targetRunTime = currentLocalTime.Date.Add(executionTime);

            if (currentLocalTime >= targetRunTime)
            {
                targetRunTime = targetRunTime.AddDays(1);
            }

            var nextDayLocal = targetRunTime.ToDushanbeTime();
            if (nextDayLocal.DayOfWeek == DayOfWeek.Sunday)
            {
                targetRunTime = targetRunTime.AddDays(1);
            }

            logger.LogInformation($"Вақти навбатии иҷро: {targetRunTime:yyyy-MM-dd HH:mm:ss}");
            return targetRunTime;
        }

        public async Task Run(CancellationToken stoppingToken = default)
        {
            try
            {
                var localNow = DateTimeOffset.UtcNow.ToDushanbeTime();
                logger.LogInformation("Оғози эҷоди дарсҳои ҳаррӯза дар {time}", localNow);

                await CheckNewlyActivatedGroups(stoppingToken);

                var tomorrow = localNow.Date.AddDays(1);
                await CreateDailyLessons(stoppingToken, tomorrow);

                logger.LogInformation($"Эҷоди дарсҳо барои рӯзи {tomorrow:yyyy-MM-dd} дар {localNow} анҷом ёфт");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Хато ҳангоми эҷоди дарсҳои ҳаррӯза: {message}", ex.Message);
            }
        }        private async Task CheckAndCreateLessonsForToday(CancellationToken stoppingToken, DateTime today)
        {
            logger.LogInformation("Санҷиши дарсҳо барои имрӯз ({today})...", today.ToString("yyyy-MM-dd"));

            var localNow = DateTimeOffset.UtcNow.ToDushanbeTime();
            if (localNow.DayOfWeek == DayOfWeek.Sunday) // Only skip on Sunday
            {
                logger.LogInformation("Имрӯз якшанбе аст, дарсҳо эҷод намешаванд");
                return;
            }

            await CreateDailyLessons(stoppingToken, today);
            logger.LogInformation("Эҷоди дарсҳо барои имрӯз ({today}) анҷом ёфт", today.ToString("yyyy-MM-dd"));
        }

        private async Task CheckNewlyActivatedGroups(CancellationToken stoppingToken)
        {
            logger.LogInformation("Санҷиши гурӯҳҳои навфаъолшуда...");

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var activatedGroups = await context.Groups
                .Where(g => g.Status == ActiveStatus.Active && !g.IsDeleted)
                .Where(g => !context.Lessons.Any(l => l.GroupId == g.Id && !l.IsDeleted))
                .ToListAsync(stoppingToken);

            if (!activatedGroups.Any())
            {
                logger.LogInformation("Гурӯҳҳои нави фаъоли бе дарс ёфт нашуданд");
                return;
            }

            logger.LogInformation($"Шумораи {activatedGroups.Count} гурӯҳи фаъоли бе дарс ёфт шуд");
            int createdCount = 0;

            foreach (var group in activatedGroups)
            {
                try
                {
                    var localNow = DateTimeOffset.UtcNow.ToDushanbeTime();
                    var baseDate = localNow.Date;
                    var lessonDate = baseDate;

                    var lesson = new Lesson
                    {
                        GroupId = group.Id,
                        WeekIndex = 1,
                        DayOfWeekIndex = 1,
                        DayIndex = 1,
                        StartTime = new DateTimeOffset(lessonDate.Year, lessonDate.Month, lessonDate.Day, 9, 0, 0, TimeSpan.Zero), // Use UTC
                        CreatedAt = DateTimeOffset.UtcNow, // Already in UTC
                        UpdatedAt = DateTimeOffset.UtcNow // Already in UTC
                    };

                    await context.Lessons.AddAsync(lesson, stoppingToken);
                    logger.LogInformation($"Барои гурӯҳи {group.Id} дарси аввалин эҷод шуд, WeekIndex: 1, DayIndex: 1");
                    createdCount++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Хато ҳангоми эҷоди дарси аввалин барои гурӯҳи {group.Id}: {ex.Message}");
                }
            }

            if (createdCount > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
                logger.LogInformation($"Шумораи {createdCount} дарси аввалин барои гурӯҳҳои фаъол сабт шуданд");
            }
        }

        private async Task CreateDailyLessons(CancellationToken stoppingToken, DateTime? targetDate = null)
        {
            var localNow = DateTimeOffset.UtcNow.ToDushanbeTime();
            var processingDate = targetDate?.Date ?? localNow.Date.AddDays(1);

            logger.LogInformation($"Эҷоди дарсҳо барои санаи {processingDate:yyyy-MM-dd} оғоз шуд...");

            if (processingDate.DayOfWeek == DayOfWeek.Sunday) // Only skip on Sunday
            {
                logger.LogInformation("Санаи коркард якшанбе аст, дарсҳо эҷод намешаванд");
                return;
            }

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var activeGroups = await context.Groups
                .Where(g => g.Status == ActiveStatus.Active && !g.IsDeleted)
                .ToListAsync(stoppingToken);

            if (!activeGroups.Any())
            {
                logger.LogInformation("Гурӯҳҳои фаъол ёфт нашуданд");
                return;
            }

            logger.LogInformation($"Шумораи {activeGroups.Count} гурӯҳи фаъол ёфт шуд");
            int createdLessonsCount = 0;
            int createdExamsCount = 0;

            foreach (var group in activeGroups)
            {
                try
                {
                    // Check if a lesson already exists for this group on the processing date
                    var lessonExistsForGroup = await context.Lessons
                        .AnyAsync(l => l.GroupId == group.Id &&
                                       !l.IsDeleted &&
                                       l.StartTime.Date == processingDate, stoppingToken);

                    if (lessonExistsForGroup)
                    {
                        logger.LogInformation($"Дарс барои гурӯҳи {group.Id} дар санаи {processingDate:yyyy-MM-dd} аллакай вуҷуд дорад");
                        continue; // Skip this group and move to the next
                    }

                    var lastLesson = await context.Lessons
                        .Where(l => l.GroupId == group.Id && !l.IsDeleted)
                        .OrderByDescending(l => l.WeekIndex)
                        .ThenByDescending(l => l.DayOfWeekIndex)
                        .FirstOrDefaultAsync(stoppingToken);

                    if (lastLesson == null)
                    {
                        logger.LogInformation($"Ягон дарси қаблӣ барои гурӯҳи {group.Id} ёфт нашуд, эҷоди дарси аввалин...");
                        var lesson = new Lesson
                        {
                            GroupId = group.Id,
                            WeekIndex = 1,
                            DayOfWeekIndex = 1,
                            DayIndex = 1,
                            StartTime = new DateTimeOffset(processingDate.Year, processingDate.Month, processingDate.Day, 9, 0, 0, TimeSpan.Zero), // Use UTC
                            CreatedAt = DateTimeOffset.UtcNow, // Already in UTC
                            UpdatedAt = DateTimeOffset.UtcNow // Already in UTC
                        };

                        await context.Lessons.AddAsync(lesson, stoppingToken);
                        createdLessonsCount++;
                        logger.LogInformation($"Барои гурӯҳи {group.Id} дарси аввалин эҷод шуд, WeekIndex: 1, DayIndex: 1");
                        continue;
                    }

                    int nextDayIndex = lastLesson.DayOfWeekIndex + 1;
                    int weekIndex = lastLesson.WeekIndex;

                    bool needExam = nextDayIndex > 5;

                    if (needExam)
                    {
                        var examExists = await context.Exams
                            .AnyAsync(e => e.GroupId == group.Id &&
                                           e.WeekIndex == weekIndex &&
                                           !e.IsDeleted,
                                      stoppingToken);

                        if (!examExists)
                        {
                            var baseDate = localNow.Date;
                            var examWeekStart = baseDate.AddDays(7 * (weekIndex - 1));
                            var examDate = examWeekStart.AddDays(5);

                            var exam = new Exam
                            {
                                GroupId = group.Id,
                                WeekIndex = weekIndex,
                                ExamDate = new DateTimeOffset(examDate.Year, examDate.Month, examDate.Day, 9, 0, 0, TimeSpan.Zero), // Use UTC
                                CreatedAt = DateTimeOffset.UtcNow, // Already in UTC
                                UpdatedAt = DateTimeOffset.UtcNow // Already in UTC
                            };

                            await context.Exams.AddAsync(exam, stoppingToken);
                            createdExamsCount++;
                            logger.LogInformation($"Барои гурӯҳи {group.Id} имтиҳон эҷод шуд, ҳафтаи {weekIndex}");
                        }

                        nextDayIndex = 1;
                        weekIndex = weekIndex + 1;

                        if (weekIndex > group.TotalWeeks)
                        {
                            weekIndex = 1;
                        }
                    }

                    int absoluteDayIndex = (weekIndex - 1) * 5 + nextDayIndex;

                    var lessonExists = await context.Lessons
                        .AnyAsync(l => l.GroupId == group.Id &&
                                       l.WeekIndex == weekIndex &&
                                       l.DayOfWeekIndex == nextDayIndex &&
                                       !l.IsDeleted,
                                  stoppingToken);

                    if (!lessonExists)
                    {
                        var lessonDate = processingDate;

                        var lesson = new Lesson
                        {
                            GroupId = group.Id,
                            WeekIndex = weekIndex,
                            DayOfWeekIndex = nextDayIndex,
                            DayIndex = absoluteDayIndex,
                            StartTime = new DateTimeOffset(lessonDate.Year, lessonDate.Month, lessonDate.Day, 9, 0, 0, TimeSpan.Zero), // Use UTC
                            CreatedAt = DateTimeOffset.UtcNow, // Already in UTC
                            UpdatedAt = DateTimeOffset.UtcNow // Already in UTC
                        };

                        await context.Lessons.AddAsync(lesson, stoppingToken);
                        createdLessonsCount++;
                        logger.LogInformation($"Барои гурӯҳи {group.Id} дарс эҷод шуд, WeekIndex: {weekIndex}, DayIndex: {nextDayIndex}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Хато ҳангоми эҷоди дарсҳо барои гурӯҳи {group.Id}: {ex.Message}");
                }
            }

            if (createdLessonsCount > 0 || createdExamsCount > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
                logger.LogInformation($"Шумораи {createdLessonsCount} дарси нав ва {createdExamsCount} имтиҳон сабт шуданд");
            }
            else
            {
                logger.LogInformation("Дарсҳо ва имтиҳонҳои нав эҷод нашуданд");
            }
        }
    }
}