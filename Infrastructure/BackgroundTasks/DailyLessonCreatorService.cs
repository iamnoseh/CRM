using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace Infrastructure.BackgroundTasks;


public class DailyLessonCreatorService(
    ILogger<DailyLessonCreatorService> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{

    public async Task Run()
    {
        logger.LogInformation("Manual run of Daily Lesson Creator triggered");
        await CreateLessonsForToday(maxRetries: 3);
        logger.LogInformation("Manual run of Daily Lesson Creator completed");
    }
    
    public async Task RunForGroup(int groupId)
    {
        logger.LogInformation($"Manual run of Daily Lesson Creator for group {groupId} triggered");
        await CreateLessonsForGroup(groupId, maxRetries: 3);
        logger.LogInformation($"Manual run of Daily Lesson Creator for group {groupId} completed");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Daily Lesson Creator Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndCreateMissingLessons(stoppingToken);

                var now = DateTimeOffset.UtcNow;
                var nextRunTime = CalculateNextRunTime(now);
                var delay = nextRunTime - now;
                
                logger.LogInformation($"Следующее плановое создание уроков в {nextRunTime} (через {delay.TotalHours:F1} часов)");

                try
                {
                    await Task.Delay(Math.Min(delay.Milliseconds, 300000), stoppingToken); 
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                
                var dayOfWeek = DateTimeOffset.UtcNow.DayOfWeek;
                if (dayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday)
                {
                    await CreateLessonsForToday(maxRetries: 3);
                }
                else
                {
                    logger.LogInformation($"Пропускаем плановое создание уроков в {dayOfWeek}");
                    await CheckAndCreateMissingLessons(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при создании ежедневных уроков");
            }
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
    {
        var today = currentTime.Date;
        var targetTime = new TimeSpan(0, 1, 0); // 00:01
        
        var targetDateTime = today.Add(targetTime);
        var targetDateTimeOffset = new DateTimeOffset(targetDateTime, currentTime.Offset);
        
        if (currentTime >= targetDateTimeOffset)
        {
            targetDateTimeOffset = targetDateTimeOffset.AddDays(1);
        }
        
        return targetDateTimeOffset;
    }
    
    private async Task CreateLessonsForToday(int maxRetries = 3, int delayBetweenRetries = 30000)
    {
        int attempts = 0;
        bool success = false;

        while (!success && attempts < maxRetries)
        {
            attempts++;
            
            try
            {
                await ExecuteLessonCreation();
                success = true;
                logger.LogInformation($"Successfully created lessons on attempt {attempts}");
            }
            catch (Exception ex)
            {
                if (attempts >= maxRetries)
                {
                    logger.LogError(ex, $"Failed to create lessons after {maxRetries} attempts. Final error: {ex.Message}");
                    throw; 
                }
                
                logger.LogWarning(ex, $"Failed to create lessons on attempt {attempts}. Will retry in {delayBetweenRetries/1000} seconds. Error: {ex.Message}");
                
                await Task.Delay(delayBetweenRetries);
            }
        }
    }
    

    private async Task CreateLessonsForGroup(int groupId, int maxRetries = 3, int delayBetweenRetries = 30000)
    {
        int attempts = 0;
        bool success = false;

        while (!success && attempts < maxRetries)
        {
            attempts++;
            
            try
            {
                await ExecuteLessonCreationForGroup(groupId);
                success = true;
                logger.LogInformation($"Successfully created lessons for group {groupId} on attempt {attempts}");
            }
            catch (Exception ex)
            {
                if (attempts >= maxRetries)
                {
                    logger.LogError(ex, $"Failed to create lessons for group {groupId} after {maxRetries} attempts. Final error: {ex.Message}");
                    throw; 
                }
                
                logger.LogWarning(ex, $"Failed to create lessons for group {groupId} on attempt {attempts}. Will retry in {delayBetweenRetries/1000} seconds. Error: {ex.Message}");

                await Task.Delay(delayBetweenRetries);
            }
        }
    }
    
    
    private async Task ExecuteLessonCreation()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        var today = DateTimeOffset.UtcNow.Date;
        var dayOfWeek = today.DayOfWeek;
        

        if (dayOfWeek < DayOfWeek.Monday || dayOfWeek > DayOfWeek.Friday)
        {
            logger.LogInformation($"Сегодня {dayOfWeek} - не создаем уроки (не рабочий день)");
            return;
        }
        var activeGroups = await context.Groups
            .Where(g => g.Started && 
                        today >= g.StartDate.Date && 
                        today <= g.EndDate.Date && 
                        !g.IsDeleted)
            .Where(g => context.StudentGroups.Any(sg => sg.GroupId == g.Id && sg.IsActive == true && !sg.IsDeleted))
            .ToListAsync();
        
        logger.LogInformation($"Найдено {activeGroups.Count} активных групп для создания уроков");
        
        int lessonCount = 0;
        
        
        foreach (var group in activeGroups)
        {
            var lastLesson = await context.Lessons
                .Where(l => l.GroupId == group.Id && !l.IsDeleted)
                .OrderByDescending(l => l.WeekIndex)
                .ThenByDescending(l => l.DayOfWeekIndex)
                .FirstOrDefaultAsync();
            
          
            int dayOfWeekIndex;
            int weekIndex;

            if (lastLesson == null)
            {
                dayOfWeekIndex = 1;
                weekIndex = 1;
            }
            else
            {
                dayOfWeekIndex = lastLesson.DayOfWeekIndex + 1; // Следующий день
                weekIndex = lastLesson.WeekIndex;
                if (dayOfWeekIndex == 6)
                {
                    var examExists = await context.Exams
                        .AnyAsync(e => e.GroupId == group.Id && 
                                     e.WeekIndex == weekIndex && 
                                     !e.IsDeleted);
                    
                    if (!examExists)
                    {
                        var exam = new Exam
                        {
                            GroupId = group.Id,
                            WeekIndex = weekIndex,
                            ExamDate = today,
                            CreatedAt = DateTimeOffset.UtcNow,
                            UpdatedAt = DateTimeOffset.UtcNow
                        };
                        
                        await context.Exams.AddAsync(exam);
                        logger.LogInformation($"Создан экзамен для группы {group.Id} на неделе {weekIndex}");
                    }
                    dayOfWeekIndex = 1;
                    weekIndex = weekIndex < 4 ? weekIndex + 1 : 1; 
                }
            }
            
            logger.LogInformation($"Создание урока для группы {group.Id}, неделя {weekIndex}, день {dayOfWeekIndex}");
             var existingLesson = await context.Lessons
                .AnyAsync(l => l.GroupId == group.Id && 
                              l.WeekIndex == weekIndex && 
                              l.DayOfWeekIndex == dayOfWeekIndex && 
                              !l.IsDeleted);
            
            if (existingLesson)
            {
                logger.LogInformation($"Урок уже существует для группы {group.Id} с индексом дня {dayOfWeekIndex}");
                continue;
            }
            var lesson = new Lesson
            {
                GroupId = group.Id,
                WeekIndex = weekIndex,
                DayOfWeekIndex = dayOfWeekIndex,
                StartTime = new DateTimeOffset(today.Year, today.Month, today.Day, 
                                            9, 0, 0, 
                                            TimeSpan.Zero), 
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            
            await context.Lessons.AddAsync(lesson);
            lessonCount++;
            logger.LogInformation($"Создан новый урок для группы {group.Id}, неделя {weekIndex}, день {dayOfWeekIndex}");
        }
        
        if (lessonCount > 0)
        {
            var savedCount = await context.SaveChangesAsync();
            logger.LogInformation($"Успешно создано {savedCount} уроков");
        }
        else
        {
            logger.LogInformation("Нет новых уроков для создания");
        }
    }
    

    private async Task ExecuteLessonCreationForGroup(int groupId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        var today = DateTimeOffset.UtcNow.Date;
        
        var group = await context.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId && 
                                     g.Started && 
                                     today >= g.StartDate.Date && 
                                     today <= g.EndDate.Date && 
                                     !g.IsDeleted && 
                                     context.StudentGroups.Any(sg => sg.GroupId == g.Id && sg.IsActive == true && !sg.IsDeleted));
        
        if (group == null)
        {
            logger.LogWarning($"Группа {groupId} не найдена или неактивна");
            return;
        }
        
        logger.LogInformation($"Создание уроков для группы {groupId}");
        
        int lessonCount = 0;
        var lastLesson = await context.Lessons
            .Where(l => l.GroupId == group.Id && !l.IsDeleted)
            .OrderByDescending(l => l.WeekIndex)
            .ThenByDescending(l => l.DayOfWeekIndex)
            .FirstOrDefaultAsync();
        
        int startDayIndex;
        int weekIndex;
        
        if (lastLesson == null)
        {
            startDayIndex = 1;
            weekIndex = 1;
        }
        else
        {
            startDayIndex = lastLesson.DayOfWeekIndex + 1; 
            weekIndex = lastLesson.WeekIndex;
            
           
            if (startDayIndex == 6)
            {
                var examExists = await context.Exams
                    .AnyAsync(e => e.GroupId == group.Id && 
                                 e.WeekIndex == weekIndex && 
                                 !e.IsDeleted);
                
                if (!examExists)
                {
                    var exam = new Exam
                    {
                        GroupId = group.Id,
                        WeekIndex = weekIndex,
                        ExamDate = today,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    
                    await context.Exams.AddAsync(exam);
                    logger.LogInformation($"Создан экзамен для группы {group.Id} на неделе {weekIndex}");
                }
                startDayIndex = 1;
                weekIndex = weekIndex < 4 ? weekIndex + 1 : 1; 
            }
        }
        
        for (int dayIndex = startDayIndex; dayIndex <= 5; dayIndex++)
        {
            var existingLesson = await context.Lessons
                .AnyAsync(l => l.GroupId == group.Id && 
                              l.WeekIndex == weekIndex && 
                              l.DayOfWeekIndex == dayIndex && 
                              !l.IsDeleted);
            
            if (existingLesson)
            {
                logger.LogInformation($"Урок уже существует для группы {group.Id}, неделя {weekIndex}, день {dayIndex}");
                continue;
            }
            
            var lesson = new Lesson
            {
                GroupId = group.Id,
                WeekIndex = weekIndex,
                DayOfWeekIndex = dayIndex,
                StartTime = new DateTimeOffset(today.Year, today.Month, today.Day, 
                                            9, 0, 0, 
                                            TimeSpan.Zero), 
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            
            await context.Lessons.AddAsync(lesson);
            lessonCount++;
            logger.LogInformation($"Создан новый урок для группы {group.Id}, неделя {weekIndex}, день {dayIndex}");
        }
        
        if (lessonCount > 0)
        {
            var savedCount = await context.SaveChangesAsync();
            logger.LogInformation($"Успешно создано {savedCount} уроков для группы {groupId}");
        }
        else
        {
            logger.LogInformation($"Нет новых уроков для создания для группы {groupId}");
        }
    }
    
    private async Task CheckAndCreateMissingLessons(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            
            var today = DateTimeOffset.UtcNow.Date;
            
            var activeGroups = await context.Groups
                .Where(g => g.Started && 
                          today >= g.StartDate.Date && 
                          today <= g.EndDate.Date && 
                          !g.IsDeleted)
                .Where(g => context.StudentGroups.Any(sg => sg.GroupId == g.Id && sg.IsActive == true && !sg.IsDeleted))
                .ToListAsync(stoppingToken);
            
            logger.LogInformation($"Проверка {activeGroups.Count} активных групп на пропущенные уроки");
            
            int createdLessonsCount = 0;
            

            foreach (var group in activeGroups)
            {
                var lastLesson = await context.Lessons
                    .Where(l => l.GroupId == group.Id && !l.IsDeleted)
                    .OrderByDescending(l => l.StartTime)
                    .FirstOrDefaultAsync(stoppingToken);
                
        
                if (lastLesson == null)
                {
                    var lesson = new Lesson
                    {
                        GroupId = group.Id,
                        WeekIndex = 1,
                        DayOfWeekIndex = 1,
                        StartTime = new DateTimeOffset(today.Year, today.Month, today.Day, 
                                                    9, 0, 0, 
                                                    TimeSpan.Zero), 
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    
                    await context.Lessons.AddAsync(lesson, stoppingToken);
                    createdLessonsCount++;
                    logger.LogInformation($"Создан первый урок для группы {group.Id}, неделя 1, день 1");
                    continue;
                }
                
               
                var latestIndexLesson = await context.Lessons
                    .Where(l => l.GroupId == group.Id && !l.IsDeleted)
                    .OrderByDescending(l => l.WeekIndex)
                    .ThenByDescending(l => l.DayOfWeekIndex)
                    .FirstOrDefaultAsync(stoppingToken);
                
                if (latestIndexLesson == null) continue; 
                
                int nextDayIndex = latestIndexLesson.DayOfWeekIndex + 1;
                int weekIndex = latestIndexLesson.WeekIndex;
                
                if (nextDayIndex == 6)
                {
                    var examExists = await context.Exams
                        .AnyAsync(e => e.GroupId == group.Id && 
                                    e.WeekIndex == weekIndex && 
                                    !e.IsDeleted, 
                                stoppingToken);
                    
                    if (!examExists)
                    {
                        var exam = new Exam
                        {
                            GroupId = group.Id,
                            WeekIndex = weekIndex,
                            ExamDate = today,
                            CreatedAt = DateTimeOffset.UtcNow,
                            UpdatedAt = DateTimeOffset.UtcNow
                        };
                        
                        await context.Exams.AddAsync(exam, stoppingToken);
                        logger.LogInformation($"Создан пропущенный экзамен для группы {group.Id}, неделя {weekIndex}");
                    }
                    
                    nextDayIndex = 1;
                    weekIndex = weekIndex < 4 ? weekIndex + 1 : 1;
                }
                
                var nextLessonExists = await context.Lessons
                    .AnyAsync(l => l.GroupId == group.Id && 
                                l.WeekIndex == weekIndex && 
                                l.DayOfWeekIndex == nextDayIndex && 
                                !l.IsDeleted, 
                             stoppingToken);
                
                if (!nextLessonExists)
                {
                    var lesson = new Lesson
                    {
                        GroupId = group.Id,
                        WeekIndex = weekIndex,
                        DayOfWeekIndex = nextDayIndex,
                        StartTime = new DateTimeOffset(today.Year, today.Month, today.Day, 
                                                    9, 0, 0, 
                                                    TimeSpan.Zero), 
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    
                    await context.Lessons.AddAsync(lesson, stoppingToken);
                    createdLessonsCount++;
                    logger.LogInformation($"Создан следующий урок для группы {group.Id}, неделя {weekIndex}, день {nextDayIndex}");
                }
            }
            
            if (createdLessonsCount > 0)
            {
                var savedCount = await context.SaveChangesAsync(stoppingToken);
                logger.LogInformation($"Создано {savedCount} пропущенных уроков");
            }
            else
            {
                logger.LogInformation("Пропущенных уроков не найдено");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при проверке пропущенных уроков");
        }
    }
}
