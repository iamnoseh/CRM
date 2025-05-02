// using System.Net;
// using Domain.DTOs.Lesson;
// using Domain.Entities;
// using Domain.Responses;
// using Infrastructure.Data;
// using Infrastructure.Interfaces;
// using Microsoft.EntityFrameworkCore;
//
// namespace Infrastructure.Services;
//
// public class LessonService (DataContext context): ILessonService
// {
//     public async Task<Response<List<GetLessonDto>>> GetLessons()
//     {
//         var lessons = await context.Lessons
//             .Include(l => l.Group)
//             .ToListAsync();
//             
//         if (lessons.Count == 0) 
//             return new Response<List<GetLessonDto>>(HttpStatusCode.NotFound, "Lessons not found");
//             
//         var dto = lessons.Select(x => new GetLessonDto
//         {
//             Id = x.Id,
//             GroupId = x.GroupId,
//             StartTime = x.StartTime,
//             WeekIndex = x.WeekIndex,
//             DayOfWeekIndex = x.DayOfWeekIndex,
//             GroupName = x.Group?.NameRu ?? "Unknown"
//         }).ToList();
//         
//         return new Response<List<GetLessonDto>>(dto);
//     }
//
//     public async Task<Response<GetLessonDto>> GetLessonById(int id)
//     {
//         var lesson = await context.Lessons
//             .Include(l => l.Group)
//             .FirstOrDefaultAsync(x => x.Id == id);
//             
//         if (lesson == null) 
//             return new Response<GetLessonDto>(HttpStatusCode.NotFound, "Lesson not found");
//             
//         var dto = new GetLessonDto
//         {
//             Id = lesson.Id,
//             GroupId = lesson.GroupId,
//             StartTime = lesson.StartTime,
//             WeekIndex = lesson.WeekIndex,
//             DayOfWeekIndex = lesson.DayOfWeekIndex,
//             GroupName = lesson.Group?.NameRu ?? "Unknown"
//         };
//         
//         return new Response<GetLessonDto>(dto);
//     }
//     
//     public async Task<Response<List<GetLessonDto>>> GetLessonsByGroup(int groupId)
//     {
//         var lessons = await context.Lessons
//             .Include(l => l.Group)
//             .Where(l => l.GroupId == groupId)
//             .OrderBy(l => l.WeekIndex)
//             .ThenBy(l => l.DayOfWeekIndex)
//             .ToListAsync();
//             
//         if (lessons.Count == 0) 
//             return new Response<List<GetLessonDto>>(HttpStatusCode.NotFound, "Lessons not found for this group");
//             
//         var dto = lessons.Select(x => new GetLessonDto
//         {
//             Id = x.Id,
//             GroupId = x.GroupId,
//             StartTime = x.StartTime,
//             WeekIndex = x.WeekIndex,
//             DayOfWeekIndex = x.DayOfWeekIndex,
//             GroupName = x.Group?.NameRu ?? "Unknown"
//         }).ToList();
//         
//         return new Response<List<GetLessonDto>>(dto);
//     }
//     
//     public async Task<Response<List<GetLessonDto>>> GetLessonsByWeek(int groupId, int weekIndex)
//     {
//         var lessons = await context.Lessons
//             .Include(l => l.Group)
//             .Where(l => l.GroupId == groupId && l.WeekIndex == weekIndex)
//             .OrderBy(l => l.DayOfWeekIndex)
//             .ToListAsync();
//             
//         if (lessons.Count == 0) 
//             return new Response<List<GetLessonDto>>(HttpStatusCode.NotFound, $"Lessons not found for group {groupId} in week {weekIndex}");
//             
//         var dto = lessons.Select(x => new GetLessonDto
//         {
//             Id = x.Id,
//             GroupId = x.GroupId,
//             StartTime = x.StartTime,
//             WeekIndex = x.WeekIndex,
//             DayOfWeekIndex = x.DayOfWeekIndex,
//             GroupName = x.Group?.NameRu ?? "Unknown"
//         }).ToList();
//         
//         return new Response<List<GetLessonDto>>(dto);
//     }
//     
//     public async Task<Response<List<GetLessonDto>>> GetLessonsByDay(int groupId, int weekIndex, int dayOfWeekIndex)
//     {
//         var lessons = await context.Lessons
//             .Include(l => l.Group)
//             .Where(l => l.GroupId == groupId && l.WeekIndex == weekIndex && l.DayOfWeekIndex == dayOfWeekIndex)
//             .ToListAsync();
//             
//         if (lessons.Count == 0) 
//             return new Response<List<GetLessonDto>>(HttpStatusCode.NotFound, $"Lessons not found for group {groupId} in week {weekIndex}, day {dayOfWeekIndex}");
//             
//         var dto = lessons.Select(x => new GetLessonDto
//         {
//             Id = x.Id,
//             GroupId = x.GroupId,
//             StartTime = x.StartTime,
//             WeekIndex = x.WeekIndex,
//             DayOfWeekIndex = x.DayOfWeekIndex,
//             GroupName = x.Group?.NameRu ?? "Unknown"
//         }).ToList();
//         
//         return new Response<List<GetLessonDto>>(dto);
//     }
//
//     public async Task<Response<string>> CreateLesson(CreateLessonDto createLessonDto)
//     {
//         // Проверяем существование группы
//         var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == createLessonDto.GroupId);
//         if (group == null)
//             return new Response<string>(HttpStatusCode.NotFound, "Group not found");
//         
//         var lesson = new Lesson
//         {
//             StartTime = createLessonDto.StartTime,
//             GroupId = createLessonDto.GroupId,
//             WeekIndex = createLessonDto.WeekIndex,
//             DayOfWeekIndex = createLessonDto.DayOfWeekIndex,
//             CreatedAt = DateTimeOffset.UtcNow,
//             UpdatedAt = DateTimeOffset.UtcNow
//         };
//         
//         context.Lessons.Add(lesson);
//         var res = await context.SaveChangesAsync();
//         
//         return res > 0 
//             ? new Response<string>(HttpStatusCode.Created, "Lesson created successfully")
//             : new Response<string>(HttpStatusCode.InternalServerError, "Lesson creation failed");
//     }
//     
//     public async Task<Response<string>> CreateWeeklyLessons(int groupId, int weekIndex, DateTimeOffset startDate)
//     {
//         // Проверяем существование группы
//         var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
//         if (group == null)
//             return new Response<string>(HttpStatusCode.NotFound, "Group not found");
//             
//         // Получаем настройки количества уроков в неделю
//         int lessonsPerWeek = group.LessonInWeek;
//         bool hasExam = group.HasWeeklyExam;
//         
//         // Создаем список новых уроков
//         var lessons = new List<Lesson>();
//         
//         // Создаем уроки на каждый день недели
//         for (int day = 1; day <= lessonsPerWeek; day++)
//         {
//             var lessonDate = startDate.AddDays(day - 1);
//             
//             var lesson = new Lesson
//             {
//                 StartTime = lessonDate,
//                 GroupId = groupId,
//                 WeekIndex = weekIndex,
//                 DayOfWeekIndex = day,
//                 CreatedAt = DateTimeOffset.UtcNow,
//                 UpdatedAt = DateTimeOffset.UtcNow
//             };
//             
//             lessons.Add(lesson);
//         }
//         
//         // Добавляем экзамен в 6-й день, если он предусмотрен
//         if (hasExam)
//         {
//             var examDate = startDate.AddDays(lessonsPerWeek);
//             
//             var exam = new Lesson
//             {
//                 StartTime = examDate,
//                 GroupId = groupId,
//                 WeekIndex = weekIndex,
//                 DayOfWeekIndex = lessonsPerWeek + 1, // 6-й день для экзамена
//                 CreatedAt = DateTimeOffset.UtcNow,
//                 UpdatedAt = DateTimeOffset.UtcNow
//             };
//             
//             lessons.Add(exam);
//         }
//         
//         await context.Lessons.AddRangeAsync(lessons);
//         var res = await context.SaveChangesAsync();
//         
//         return res > 0 
//             ? new Response<string>(HttpStatusCode.Created, $"Created {lessons.Count} lessons for week {weekIndex}")
//             : new Response<string>(HttpStatusCode.InternalServerError, "Failed to create weekly lessons");
//     }
//
//     public async Task<Response<string>> UpdateLesson(UpdateLessonDto updateLessonDto)
//     {
//         var lesson = await context.Lessons.FirstOrDefaultAsync(x => x.Id == updateLessonDto.LessonId);
//         if (lesson == null) 
//             return new Response<string>(HttpStatusCode.NotFound, "Lesson not found");
//             
//         lesson.StartTime = updateLessonDto.StartTime;
//         lesson.WeekIndex = updateLessonDto.WeekIndex;
//         lesson.DayOfWeekIndex = updateLessonDto.DayOfWeekIndex;
//         lesson.UpdatedAt = DateTimeOffset.UtcNow;
//         
//         var res = await context.SaveChangesAsync();
//         
//         return res > 0 
//             ? new Response<string>(HttpStatusCode.OK, "Lesson updated successfully")
//             : new Response<string>(HttpStatusCode.BadRequest, "Lesson update failed");
//     }
//
//     public async Task<Response<string>> DeleteLesson(int id)
//     {
//         var lesson = await context.Lessons.FirstOrDefaultAsync(x => x.Id == id);
//         if (lesson == null) 
//             return new Response<string>(HttpStatusCode.NotFound, "Lesson not found");
//             
//         context.Lessons.Remove(lesson);
//         var res = await context.SaveChangesAsync();
//         
//         return res > 0 
//             ? new Response<string>(HttpStatusCode.OK, "Lesson deleted successfully")
//             : new Response<string>(HttpStatusCode.InternalServerError, "Lesson deletion failed");
//     }
// }