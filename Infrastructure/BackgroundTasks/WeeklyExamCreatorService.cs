using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundTasks;

/// <summary>
/// Фоновая служба для автоматического создания еженедельных экзаменов после создания уроков
/// </summary>
public class WeeklyExamCreatorService : BackgroundService
{
    private readonly ILogger<WeeklyExamCreatorService> logger;
    private readonly IServiceProvider _serviceProvider;

    public WeeklyExamCreatorService(
        ILogger<WeeklyExamCreatorService> logger,
        IServiceProvider serviceProvider)
    {
        this.logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Weekly Exam Creator Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Получаем текущее время
                var now = DateTimeOffset.UtcNow;
                
                // Создаем экзамены по субботам в 00:04 (после создания уроков в 00:01)
                var nextRunTime = CalculateNextRunTime(now);
                var delay = nextRunTime - now;
                
                logger.LogInformation($"Next exam creation scheduled at {nextRunTime} (in {delay.TotalHours:F1} hours)");
                
                // Ждем до следующего запланированного запуска
                await Task.Delay(delay, stoppingToken);
                
                // Проверяем, суббота ли сегодня
                var dayOfWeek = DateTimeOffset.UtcNow.DayOfWeek;
                if (dayOfWeek == DayOfWeek.Saturday)
                {
                    await CreateWeeklyExams();
                }
                else
                {
                    logger.LogInformation($"Skipping exam creation on {dayOfWeek}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while creating weekly exams");
            }
            
            // Ждем некоторое время перед следующей итерацией цикла
            // Это предотвращает слишком частые проверки, если произошла ошибка
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Подавляем исключение TaskCanceledException при остановке сервиса
                break;
            }
        }
    }

    /// <summary>
    /// Вычисляет время следующего запуска (суббота, 00:04)
    /// </summary>
    private DateTimeOffset CalculateNextRunTime(DateTimeOffset currentTime)
    {
        // Текущий день
        var today = currentTime.Date;
        var dayOfWeek = today.DayOfWeek;
        
        // Целевое время - суббота, 00:04
        var targetTime = new TimeSpan(0, 4, 0); // 00:04
        
        // Сколько дней до следующей субботы
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)dayOfWeek + 7) % 7;
        if (daysUntilSaturday == 0 && currentTime.TimeOfDay > targetTime)
        {
            // Если сегодня суббота, но уже после 00:04, берем следующую субботу
            daysUntilSaturday = 7;
        }
        
        var nextSaturday = today.AddDays(daysUntilSaturday);
        var targetDateTime = nextSaturday.Add(targetTime);
        
        return new DateTimeOffset(targetDateTime, currentTime.Offset);
    }

    /// <summary>
    /// Создает экзамены для всех активных групп на текущую неделю
    /// </summary>
    private async Task CreateWeeklyExams()
    {
        // Используем скоуп для получения необходимых сервисов
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        var today = DateTimeOffset.UtcNow.Date;
        
        try
        {
            // Получаем все активные группы
            var activeGroups = await context.Groups
                .Where(g => g.Started && 
                          today >= g.StartDate.Date && 
                          today <= g.EndDate.Date && 
                          !g.IsDeleted)
                .ToListAsync();
            
            logger.LogInformation($"Found {activeGroups.Count} active groups for exam creation");
            
            // Определяем текущую неделю обучения для каждой группы
            foreach (var group in activeGroups)
            {
                // Вычисляем индекс предыдущей недели относительно даты начала группы
                var weeksSinceStart = (int)Math.Floor((today - group.StartDate.Date).TotalDays / 7);
                var previousWeekIndex = weeksSinceStart % 4 + 1; // Индекс недели, которая только что закончилась
                
                logger.LogInformation($"Creating exam for group {group.Id} for week {previousWeekIndex}");
                
                // Проверяем, нет ли уже экзамена для этой недели для этой группы
                var existingExam = await context.Exams
                    .AnyAsync(e => e.GroupId == group.Id && 
                                  e.WeekIndex == previousWeekIndex && 
                                  !e.IsDeleted);
                
                if (existingExam)
                {
                    logger.LogInformation($"Exam already exists for group {group.Id} for week {previousWeekIndex}");
                    continue;
                }
                
                // Определяем дату экзамена (сегодняшняя суббота)
                var examDate = new DateTimeOffset(today.Year, today.Month, today.Day,
                                               10, 0, 0,
                                               TimeSpan.Zero); // Используем UTC (TimeSpan.Zero)
                
                // Создаем экзамен для предыдущей недели
                var exam = new Exam
                {
                    GroupId = group.Id,
                    WeekIndex = previousWeekIndex,
                    ExamDate = examDate,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                
                await context.Exams.AddAsync(exam);
                logger.LogInformation($"Created new exam for group {group.Id} for week {previousWeekIndex}");
                
                // Создаем записи ExamGrade для всех студентов в группе
                var studentsInGroup = await context.StudentGroups
                    .Where(sg => sg.GroupId == group.Id && 
                               (bool)sg.IsActive &&
                               !sg.IsDeleted)
                    .Select(sg => sg.StudentId)
                    .ToListAsync();
                
                foreach (var studentId in studentsInGroup)
                {
                    var examGrade = new ExamGrade
                    {
                        ExamId = exam.Id,
                        StudentId = studentId,
                        Points = 0, // Изначально пусто, будет заполнено преподавателем
                        Comment = null,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    
                    await context.ExamGrades.AddAsync(examGrade);
                }
                
                logger.LogInformation($"Created {studentsInGroup.Count} exam grade entries for students in group {group.Id}");
            }
            
            // Сохраняем изменения
            var savedCount = await context.SaveChangesAsync();
            logger.LogInformation($"Successfully created exams and grades");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating weekly exams");
            throw;
        }
    }
}
