using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
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
                    var now = DateTimeOffset.UtcNow;
                    var today = now.Date;

                    // Санҷиши дарсҳо барои имрӯз
                    await CheckAndCreateLessonsForToday(stoppingToken, today);

                    // Ҳисоб кардани вақти иҷрои навбатӣ
                    var nextRun = CalculateNextRunTime(now);
                    var delay = nextRun - now;

                    if (delay <= TimeSpan.Zero)
                    {
                        delay = TimeSpan.Zero;
                    }

                    logger.LogInformation("Next lesson creation scheduled for {nextRun} (in {delay})", 
                        nextRun, delay);

                    await Task.Delay(delay, stoppingToken);

                    if (now.DayOfWeek != DayOfWeek.Saturday && now.DayOfWeek != DayOfWeek.Sunday)
                    {
                        await Run(stoppingToken);
                    }
                    else
                    {
                        logger.LogInformation("Skipping lesson creation - it's weekend");
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in DailyLessonCreatorService: {message}", ex.Message);
                    
                    // Интизорӣ 5 дақиқа пеш аз кӯшиши навбатӣ
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
                logger.LogInformation("Оғози эҷоди дарсҳои ҳаррӯза дар {time}", DateTimeOffset.UtcNow);
                
                // Гурӯҳҳои навро санҷида, барои онҳо дарсҳои аввалро эҷод мекунем
                await CheckNewlyActivatedGroups(stoppingToken);
                
                // Танҳо барои рӯзи пагоҳ дарсҳои ҳаррӯза эҷод мекунем
                var tomorrow = DateTimeOffset.UtcNow.Date.AddDays(1);
                await CreateDailyLessons(stoppingToken, tomorrow);
                
                logger.LogInformation($"Эҷоди дарсҳо барои рӯзи {tomorrow:yyyy-MM-dd} дар {DateTimeOffset.UtcNow} анҷом ёфт");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Хато ҳангоми эҷоди дарсҳои ҳаррӯза: {message}", ex.Message);
            }
        }

        /// <summary>
        /// Санҷиши вуҷуди дарсҳо барои имрӯз ва эҷоди онҳо дар сурати зарурат
        /// </summary>
        private async Task CheckAndCreateLessonsForToday(CancellationToken stoppingToken, DateTime today)
        {
            logger.LogInformation("Санҷиши дарсҳо барои имрӯз ({today})...", today.ToString("yyyy-MM-dd"));

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            // Санҷиши вуҷуди дарсҳо барои имрӯз
            var lessonsExist = await context.Lessons
                .AnyAsync(l => !l.IsDeleted && l.StartTime.Date == today, stoppingToken);

            if (lessonsExist)
            {
                logger.LogInformation("Дарсҳо барои имрӯз аллакай эҷод шудаанд");
                return;
            }

            logger.LogInformation("Дарсҳо барои имрӯз ёфт нашуданд, оғози эҷоди дарсҳо...");

            // Агар рӯзи имрӯз шанбе ё якшанбе бошад, дарс эҷод намешавад
            if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                logger.LogInformation("Имрӯз рӯзи истироҳат аст, дарсҳо эҷод намешаванд");
                return;
            }

            // Эҷоди дарсҳо барои имрӯз
            await CreateDailyLessons(stoppingToken, today);
            logger.LogInformation("Эҷоди дарсҳо барои имрӯз ({today}) анҷом ёфт", today.ToString("yyyy-MM-dd"));
        }

        /// <summary>
        /// Вақти оғози навбатиро ҳисоб мекунад (танҳо як маротиба дар рӯз соати 00:01)
        /// </summary>
        private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
        {
            // Танҳо як вақти иҷро дар рӯз - соати 00:01
            var executionTime = new TimeSpan(0, 1, 0); // 00:01 AM
            
            // Вақти иҷро барои имрӯз
            var targetRunTime = currentTime.Date.Add(executionTime);
            
            // Агар аз вақти иҷрои имрӯз гузашта бошад, ба рӯзи оянда мегузарем
            if (currentTime >= targetRunTime)
            {
                targetRunTime = targetRunTime.AddDays(1);
            }
            
            // Санҷиши рӯзи ҳафта (агар рӯзи истироҳат бошад, ба душанбе мегузарем)
            var dayOfWeek = targetRunTime.DayOfWeek;
            if (dayOfWeek == DayOfWeek.Saturday)
            {
                // Аз шанбе ба душанбе (иловаи 2 рӯз)
                targetRunTime = targetRunTime.AddDays(2);
            }
            else if (dayOfWeek == DayOfWeek.Sunday)
            {
                // Аз якшанбе ба душанбе (иловаи 1 рӯз)
                targetRunTime = targetRunTime.AddDays(1);
            }
            
            logger.LogInformation($"Вақти навбатии иҷро: {targetRunTime:yyyy-MM-dd HH:mm:ss}");
            return targetRunTime;
        }

        /// <summary>
        /// Гурӯҳҳои навфаъолшударо месанҷад ва барои онҳо дарсҳои аввалин эҷод мекунад
        /// </summary>
        private async Task CheckNewlyActivatedGroups(CancellationToken stoppingToken)
        {
            logger.LogInformation("Санҷиши гурӯҳҳои навфа'олшуда...");
            
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            // Гурӯҳҳои фаъоли бе дарсро мегирем
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
                    // Барои гурӯҳ дарси аввалинро эҷод мекунем
                    var baseDate = DateTimeOffset.UtcNow.Date;
                    
                    // Барои дарси аввал санаи имрӯзро истифода мебарем
                    var lessonDate = baseDate; // Дарс аз имрӯз сар мешавад
                    
                    var lesson = new Lesson
                    {
                        GroupId = group.Id,
                        WeekIndex = 1,
                        DayOfWeekIndex = 1,
                        DayIndex = 1, // Рӯзи аввал
                        StartTime = new DateTimeOffset(lessonDate.Year, lessonDate.Month, lessonDate.Day, 9, 0, 0, TimeSpan.Zero),
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
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

        /// <summary>
        /// Барои ҳамаи гурӯҳҳои фаъол дарсҳои ҳаррӯза эҷод мекунад
        /// </summary>
        private async Task CreateDailyLessons(CancellationToken stoppingToken, DateTime? targetDate = null)
        {
            // Санаи коркард (ё имрӯз, ё санаи додашуда)
            var processingDate = targetDate?.Date ?? DateTimeOffset.UtcNow.Date.AddDays(1);
            
            logger.LogInformation($"Эҷоди дарсҳо барои санаи {processingDate:yyyy-MM-dd} оғоз шуд...");

            // Агар санаи коркард рӯзи якшанбе ё шанбе бошад, дарсҳо эҷод намешаванд
            if (processingDate.DayOfWeek == DayOfWeek.Saturday || processingDate.DayOfWeek == DayOfWeek.Sunday)
            {
                logger.LogInformation("Санаи коркард рӯзи истироҳат аст, дарсҳо эҷод намешаванд");
                return;
            }
            
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            // Ҳамаи гурӯҳҳои фаъолро мегирем
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
                    // Дарси охирини гурӯҳро мегирем
                    var lastLesson = await context.Lessons
                        .Where(l => l.GroupId == group.Id && !l.IsDeleted)
                        .OrderByDescending(l => l.WeekIndex)
                        .ThenByDescending(l => l.DayOfWeekIndex)
                        .FirstOrDefaultAsync(stoppingToken);
                        
                    if (lastLesson == null)
                    {
                        // Агар дарс набошад, бо усули CheckNewlyActivatedGroups эҷод мешавад
                        continue;
                    }
                    
                    // Индексҳои навбатиро ҳисоб мекунем
                    int nextDayIndex = lastLesson.DayOfWeekIndex + 1;
                    int weekIndex = lastLesson.WeekIndex;
                    
                    // Месанҷем, ки оё dayIndex ба қимати 5 расидааст (пас аз он имтиҳон лозим аст)
                    bool needExam = nextDayIndex > 5;
                    
                    if (needExam)
                    {
                        // Месанҷем, ки оё барои ҳафтаи ҷорӣ имтиҳон мавҷуд аст
                        var examExists = await context.Exams
                            .AnyAsync(e => e.GroupId == group.Id && 
                                         e.WeekIndex == weekIndex && 
                                         !e.IsDeleted,
                                       stoppingToken);
                                       
                        if (!examExists)
                        {
                            // Барои ҳафтаи ҷорӣ имтиҳон эҷод мекунем
                            var baseDate = DateTimeOffset.UtcNow.Date;
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
                            logger.LogInformation($"Барои гурӯҳи {group.Id} имтиҳон эҷод шуд, ҳафтаи {weekIndex}");
                        }
                        
                        // Ба ҳафтаи оянда мегузарем
                        nextDayIndex = 1;
                        weekIndex = weekIndex + 1;
                        
                        // Агар ба ҳадди аксар расидем, ба ҳафтаи 1 бармегардем
                        if (weekIndex > group.TotalWeeks)
                        {
                            weekIndex = 1;
                        }
                    }
                    
                    // Ҳисоб кардани индекси умумии рӯз
                    int absoluteDayIndex = (weekIndex - 1) * 5 + nextDayIndex;
                    
                    // Месанҷем, ки оё дарс бо чунин индексҳо мавҷуд аст
                    var lessonExists = await context.Lessons
                        .AnyAsync(l => l.GroupId == group.Id && 
                                    l.WeekIndex == weekIndex && 
                                    l.DayOfWeekIndex == nextDayIndex && 
                                    !l.IsDeleted,
                                  stoppingToken);
                                  
                    if (!lessonExists)
                    {
                        // Дарси нав эҷод мекунем
                        var lessonDate = processingDate;
                        
                        var lesson = new Lesson
                        {
                            GroupId = group.Id,
                            WeekIndex = weekIndex,
                            DayOfWeekIndex = nextDayIndex,
                            DayIndex = absoluteDayIndex,
                            StartTime = new DateTimeOffset(lessonDate.Year, lessonDate.Month, lessonDate.Day, 9, 0, 0, TimeSpan.Zero),
                            CreatedAt = DateTimeOffset.UtcNow,
                            UpdatedAt = DateTimeOffset.UtcNow
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