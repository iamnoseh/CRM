// using Domain.Entities;
// using Infrastructure.Data;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
//
// namespace Infrastructure.BackgroundTasks;
//
// public class LessonSchedulerService
// {
//     private readonly DataContext _context;
//     private readonly ILogger<LessonSchedulerService> _logger;
//
//     public LessonSchedulerService(DataContext context, ILogger<LessonSchedulerService> logger)
//     {
//         _context = context;
//         _logger = logger;
//     }
//     
//     public async Task RunAsync()
//     {
//         try
//         {
//             var today = DateTimeOffset.UtcNow;
//             var dayOfWeek = today.DayOfWeek;
//
//             // Воскресенье , пропускаем планирование
//             if (dayOfWeek == DayOfWeek.Sunday)
//             {
//                 _logger.LogInformation("Skipping lesson scheduling on Sunday.");
//                 return;
//             }
//
//             // Получаем все активные группы
//             var activeGroups = await _context.Groups
//                 .Where(g => g.Started
//                             && today >= g.StartDate.Date
//                             && today <= g.EndDate.Date
//                             && !g.IsDeleted)
//                 .ToListAsync();
//
//             _logger.LogInformation($"Found {activeGroups.Count} active groups for scheduling.");
//
//             foreach (var group in activeGroups)
//             {
//                 // С понедельника по пятницу создаем уроки
//                 if (dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Friday)
//                 {
//                     await CreateLessonForGroup(group, today, dayOfWeek);
//                 }
//                 // В субботу создаем экзамены и обновляем номер недели
//                 else if (dayOfWeek == DayOfWeek.Saturday)
//                 {
//                     await CreateExamsForGroup(group, today);
//                     await UpdateGroupWeek(group);
//                 }
//             }
//
//             await _context.SaveChangesAsync();
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error occurred during lesson scheduling.");
//         }
//     }
//
//     private async Task CreateLessonForGroup(Group group, DateTimeOffset today, DayOfWeek dayOfWeek)
//     {
//         try
//         {
//             // Проверяем, существует ли уже урок на этот день
//             bool lessonExists = await _context.Lessons
//                 .AnyAsync(l => l.GroupId == group.Id 
//                         && l.StartTime.Date == today.Date
//                         && l.WeekIndex == group.CurrentWeek);
//
//             if (!lessonExists)
//             {
//                 var dayIndex = (int)dayOfWeek; // DayOfWeek enum: Monday = 1, Tuesday = 2, etc.
//                 
//                 var lesson = new Lesson
//                 {
//                     GroupId = group.Id,
//                     StartTime = today,
//                     WeekIndex = group.CurrentWeek,
//                     DayOfWeekIndex = dayIndex,
//                     CreatedAt = DateTimeOffset.UtcNow,
//                     UpdatedAt = DateTimeOffset.UtcNow
//                 };
//                 _context.Lessons.Add(lesson);
//                 
//                 _logger.LogInformation($"Created lesson for group {group.Id}, week {group.CurrentWeek}, day {dayIndex}");
//                 
//                 // Также сразу создаем записи оценок для всех студентов группы
//                 await CreateGradesForLesson(group.Id, lesson);
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, $"Error creating lesson for group {group.Id}");
//         }
//     }
//     
//     private async Task CreateGradesForLesson(int groupId, Lesson lesson)
//     {
//         try
//         {
//             // Получаем всех студентов группы
//             var students = await _context.StudentGroups
//                 .Where(sg => sg.GroupId == groupId)
//                 .Select(sg => sg.Student)
//                 .ToListAsync();
//                 
//             if (students.Any())
//             {
//                 foreach (var student in students)
//                 {
//                     // Создаем пустую оценку для каждого студента
//                     var grade = new Grade
//                     {
//                         StudentId = student.Id,
//                         GroupId = groupId,
//                         LessonId = lesson.Id,
//                         WeekIndex = lesson.WeekIndex,
//                         Value = null, // Пустая оценка, будет заполнена преподавателем
//                         BonusPoints = null,
//                         Comment = null,
//                         CreatedAt = DateTimeOffset.UtcNow,
//                         UpdatedAt = DateTimeOffset.UtcNow
//                     };
//                     
//                     _context.Grades.Add(grade);
//                 }
//                 
//                 _logger.LogInformation($"Created {students.Count} grade records for lesson {lesson.Id}");
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, $"Error creating grades for lesson {lesson.Id}");
//         }
//     }
//
//     private async Task CreateExamsForGroup(Group group, DateTimeOffset today)
//     {
//         try
//         {
//             // Получаем всех студентов группы
//             var students = await _context.StudentGroups
//                 .Where(sg => sg.GroupId == group.Id)
//                 .Select(sg => sg.Student)
//                 .ToListAsync();
//
//             int examsCreated = 0;
//             
//             // Создаем записи экзаменов для каждого студента на текущую неделю
//             foreach (var student in students)
//             {
//                 // Избегаем дубликатов экзаменов
//                 bool examExists = await _context.Exams
//                     .AnyAsync(e => e.StudentId == student.Id 
//                                 && e.GroupId == group.Id 
//                                 && e.WeekIndex == group.CurrentWeek);
//                 
//                 if (!examExists)
//                 {
//                     var exam = new Exam
//                     {
//                         StudentId = student.Id,
//                         GroupId = group.Id,
//                         WeekIndex = group.CurrentWeek,
//                         Value = null,
//                         BonusPoints = null,
//                         Comment = null,
//                         CreatedAt = DateTimeOffset.UtcNow,
//                         UpdatedAt = DateTimeOffset.UtcNow
//                     };
//                     _context.Exams.Add(exam);
//                     examsCreated++;
//                 }
//             }
//             
//             if (examsCreated > 0)
//             {
//                 _logger.LogInformation($"Created {examsCreated} exams for group {group.Id}, week {group.CurrentWeek}");
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, $"Error creating exams for group {group.Id}");
//         }
//     }
//     
//     private async Task UpdateGroupWeek(Group group)
//     {
//         try
//         {
//             // Инкремент текущей недели после субботы (все 6 дней выполнены)
//             int maxWeeks = group.DurationMonth * 4; // Предполагаем 4 недели в месяц
//             if (group.CurrentWeek < maxWeeks)
//             {
//                 group.CurrentWeek += 1;
//                 _logger.LogInformation($"Incremented week for group {group.Id} to {group.CurrentWeek}");
//             }
//             else if (group.CurrentWeek >= maxWeeks)
//             {
//                 // Курс закончен
//                 group.Started = false;
//                 _logger.LogInformation($"Group {group.Id} has completed all {maxWeeks} weeks and is now marked as finished.");
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, $"Error updating week for group {group.Id}");
//         }
//     }
// }