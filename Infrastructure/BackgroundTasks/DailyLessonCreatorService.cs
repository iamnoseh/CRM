using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundTasks
{
    public class DailyLessonCreatorService : BackgroundService
    {
        private readonly ILogger<DailyLessonCreatorService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _localOffset = TimeSpan.FromHours(5); // Вақти маҳаллӣ (+05:00)

        public DailyLessonCreatorService(
            ILogger<DailyLessonCreatorService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DailyLessonCreatorService started at {time}", DateTimeOffset.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var utcNow = DateTimeOffset.UtcNow;
                    var localNow = utcNow + _localOffset;
                    var today = localNow.Date;

                    _logger.LogInformation("Рӯзи ҷорӣ: {dayOfWeek} ({today})", localNow.DayOfWeek, today.ToString("yyyy-MM-dd"));

                    await CheckAndCreateLessonsForToday(stoppingToken, today);

                    var nextRun = CalculateNextRunTime(localNow);
                    var delay = nextRun - utcNow;
                    if (delay <= TimeSpan.Zero)
                    {
                        delay = TimeSpan.FromSeconds(1);
                    }

                    _logger.LogInformation("Next lesson creation scheduled for {nextRun} (in {delay})", nextRun, delay);

                    await Task.Delay(delay, stoppingToken);

                    if (localNow.DayOfWeek != DayOfWeek.Sunday)
                    {
                        await Run(stoppingToken);
                    }
                    else
                    {
                        _logger.LogInformation("Skipping lesson creation - it's weekend");
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in DailyLessonCreatorService: {message}", ex.Message);
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        public async Task Run(CancellationToken stoppingToken = default)
        {
            try
            {
                var localNow = DateTimeOffset.UtcNow + _localOffset;
                _logger.LogInformation("Оғози эҷоди дарсҳои ҳаррӯза дар {time}", localNow);

                await CheckNewlyActivatedGroups(stoppingToken);

                var tomorrow = localNow.Date.AddDays(1);
                await CreateDailyLessons(stoppingToken, tomorrow);

                _logger.LogInformation($"Эҷоди дарсҳо барои рӯзи {tomorrow:yyyy-MM-dd} дар {localNow} анҷом ёфт");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Хато ҳангоми эҷоди дарсҳои ҳаррӯза: {message}", ex.Message);
            }
        }

        private async Task CheckAndCreateLessonsForToday(CancellationToken stoppingToken, DateTime today)
        {
            _logger.LogInformation("Санҷиши дарсҳо барои имрӯз ({today})...", today.ToString("yyyy-MM-dd"));

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var lessonsExist = await context.Lessons
                .AnyAsync(l => !l.IsDeleted && l.StartTime.Date == today, stoppingToken);

            if (lessonsExist)
            {
                _logger.LogInformation("Дарсҳо барои имрӯз аллакай эҷод шудаанд");
                return;
            }

            _logger.LogInformation("Дарсҳо барои имрӯз ёфт нашуданд, оғози эҷоди дарсҳо...");

            var localNow = DateTimeOffset.UtcNow + _localOffset;
            if (localNow.DayOfWeek == DayOfWeek.Saturday || localNow.DayOfWeek == DayOfWeek.Sunday)
            {
                _logger.LogInformation("Имрӯз рӯзи истироҳат аст, дарсҳо эҷод намешаванд");
                return;
            }

            await CreateDailyLessons(stoppingToken, today);
            _logger.LogInformation("Эҷоди дарсҳо барои имрӯз ({today}) анҷом ёфт", today.ToString("yyyy-MM-dd"));
        }

        private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentLocalTime)
        {
            var executionTime = new TimeSpan(0, 1, 0);
            var targetRunTime = currentLocalTime.Date.Add(executionTime);

            if (currentLocalTime >= targetRunTime)
            {
                targetRunTime = targetRunTime.AddDays(1);
            }

            var dayOfWeek = (targetRunTime + _localOffset).DayOfWeek;

             if (dayOfWeek == DayOfWeek.Sunday)
            {
                targetRunTime = targetRunTime.AddDays(1);
            }

            _logger.LogInformation($"Вақти навбатии иҷро: {targetRunTime:yyyy-MM-dd HH:mm:ss}");
            return targetRunTime;
        }

        private async Task CheckNewlyActivatedGroups(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Санҷиши гурӯҳҳои навфаъолшуда...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var activatedGroups = await context.Groups
                .Where(g => g.Status == ActiveStatus.Active && !g.IsDeleted)
                .Where(g => !context.Lessons.Any(l => l.GroupId == g.Id && !l.IsDeleted))
                .ToListAsync(stoppingToken);

            if (!activatedGroups.Any())
            {
                _logger.LogInformation("Гурӯҳҳои нави фаъоли бе дарс ёфт нашуданд");
                return;
            }

            int createdCount = 0;
            foreach (var group in activatedGroups)
            {
                try
                {
                    var localNow = DateTimeOffset.UtcNow + _localOffset;
                    var baseDate = localNow.Date;
                    var lessonDate = baseDate;

                    var lesson = new Lesson
                    {
                        GroupId = group.Id,
                        WeekIndex = 1,
                        DayOfWeekIndex = 1,
                        DayIndex = 1,
                        StartTime = new DateTimeOffset(lessonDate.Year, lessonDate.Month, lessonDate.Day, 9, 0, 0, _localOffset),
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };

                    await context.Lessons.AddAsync(lesson, stoppingToken);
                    createdCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Хато ҳангоми эҷоди дарси аввалин барои гурӯҳи {group.Id}: {ex.Message}");
                }
            }

            if (createdCount > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"Шумораи {createdCount} дарси аввалин барои гурӯҳҳои фаъол сабт шуданд");
            }
        }

        private async Task CreateDailyLessons(CancellationToken stoppingToken, DateTime? targetDate = null)
        {
            var localNow = DateTimeOffset.UtcNow + _localOffset;
            var processingDate = targetDate?.Date ?? localNow.Date.AddDays(1);

            if (processingDate.DayOfWeek == DayOfWeek.Sunday)
            {
                _logger.LogInformation("Санаи коркард рӯзи истироҳат аст, дарсҳо эҷод намешаванд");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var activeGroups = await context.Groups
                .Where(g => g.Status == ActiveStatus.Active && !g.IsDeleted)
                .ToListAsync(stoppingToken);

            int createdLessonsCount = 0;
            int createdExamsCount = 0;

            foreach (var group in activeGroups)
            {
                try
                {
                    var lastLesson = await context.Lessons
                        .Where(l => l.GroupId == group.Id && !l.IsDeleted)
                        .OrderByDescending(l => l.WeekIndex)
                        .ThenByDescending(l => l.DayOfWeekIndex)
                        .FirstOrDefaultAsync(stoppingToken);

                    if (lastLesson == null) continue;

                    int nextDayIndex = lastLesson.DayOfWeekIndex + 1;
                    int weekIndex = lastLesson.WeekIndex;
                    bool needExam = nextDayIndex > 5;

                    if (needExam)
                    {
                        var examExists = await context.Exams.AnyAsync(e => e.GroupId == group.Id && e.WeekIndex == weekIndex && !e.IsDeleted, stoppingToken);
                        if (!examExists)
                        {
                            var baseDate = localNow.Date;
                            var examWeekStart = baseDate.AddDays(7 * (weekIndex - 1));
                            var examDate = examWeekStart.AddDays(5);

                            var exam = new Exam
                            {
                                GroupId = group.Id,
                                WeekIndex = weekIndex,
                                ExamDate = examDate,
                                CreatedAt = DateTimeOffset.UtcNow,
                                UpdatedAt = DateTimeOffset.UtcNow
                            };

                            await context.Exams.AddAsync(exam, stoppingToken);
                            createdExamsCount++;
                        }

                        nextDayIndex = 1;
                        weekIndex++;

                        if (weekIndex > group.TotalWeeks)
                        {
                            weekIndex = 1;
                        }
                    }

                    int absoluteDayIndex = (weekIndex - 1) * 5 + nextDayIndex;
                    bool lessonExists = await context.Lessons.AnyAsync(l => l.GroupId == group.Id && l.WeekIndex == weekIndex && l.DayOfWeekIndex == nextDayIndex && !l.IsDeleted, stoppingToken);
                    if (!lessonExists)
                    {
                        var lessonDate = processingDate;
                        var lesson = new Lesson
                        {
                            GroupId = group.Id,
                            WeekIndex = weekIndex,
                            DayOfWeekIndex = nextDayIndex,
                            DayIndex = absoluteDayIndex,
                            StartTime = new DateTimeOffset(lessonDate.Year, lessonDate.Month, lessonDate.Day, 9, 0, 0, _localOffset),
                            CreatedAt = DateTimeOffset.UtcNow,
                            UpdatedAt = DateTimeOffset.UtcNow
                        };

                        await context.Lessons.AddAsync(lesson, stoppingToken);
                        createdLessonsCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Хато ҳангоми эҷоди дарсҳо барои гурӯҳи {group.Id}: {ex.Message}");
                }
            }

            if (createdLessonsCount > 0 || createdExamsCount > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"Шумораи {createdLessonsCount} дарси нав ва {createdExamsCount} имтиҳон сабт шуданд");
            }
            else
            {
                _logger.LogInformation("Дарсҳо ва имтиҳонҳои нав эҷод нашуданд");
            }
        }
    }
}
