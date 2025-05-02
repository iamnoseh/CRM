// using System.Net;
// using Domain.DTOs.Grade;
// using Domain.Entities;
// using Domain.Responses;
// using Infrastructure.Data;
// using Infrastructure.Interfaces;
// using Microsoft.EntityFrameworkCore;
//
// namespace Infrastructure.Services;
//
// public class GradeService(DataContext context) : IGradeService
// {
//     #region GetGradeByIdAsync
//     public async Task<Response<GetGradeDto>> GetGradeByIdAsync(int id)
//     {
//         var grade = await context.Grades
//             .Include(g => g.Student)
//             .Include(g => g.Group)
//             .Include(g => g.Lesson)
//             .FirstOrDefaultAsync(x => x.Id == id);
//             
//         if (grade == null) 
//             return new Response<GetGradeDto>(HttpStatusCode.NotFound, "Grade not found");
//             
//         var dto = new GetGradeDto
//         {
//             Id = grade.Id,
//             GroupId = grade.GroupId,
//             Comment = grade.Comment,
//             StudentId = grade.StudentId,
//             Value = grade.Value,
//             BonusPoints = grade.BonusPoints,
//             LessonId = grade.LessonId,
//             WeekIndex = grade.WeekIndex
//         };
//         
//         return new Response<GetGradeDto>(dto);
//     }
//     
//     #endregion
//
//     #region GetGrades
//     public async Task<Response<List<GetGradeDto>>> GetAllGradesAsync()
//     {
//         var grades = await context.Grades
//             .Include(g => g.Student)
//             .Include(g => g.Group)
//             .Include(g => g.Lesson)
//             .ToListAsync();
//             
//         if (grades.Count == 0) 
//             return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "No Grades found");
//             
//         var dto = grades.Select(x => new GetGradeDto
//         {
//             Id = x.Id,
//             GroupId = x.GroupId,
//             Comment = x.Comment,
//             StudentId = x.StudentId,
//             Value = x.Value,
//             BonusPoints = x.BonusPoints,
//             LessonId = x.LessonId,
//             WeekIndex = x.WeekIndex
//         }).ToList();
//         
//         return new Response<List<GetGradeDto>>(dto);
//     }
//     #endregion
//     
//     #region GetGradesByStudentId
//     public async Task<Response<List<GetGradeDto>>> GetGradesByStudentAsync(int studentId)
//     {
//         var grades = await context.Grades
//             .Include(g => g.Student)
//             .Include(g => g.Group)
//             .Include(g => g.Lesson)
//             .Where(g => g.StudentId == studentId)
//             .ToListAsync();
//             
//         if (grades.Count == 0) 
//             return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "No grades found for this student");
//             
//         var dto = grades.Select(x => new GetGradeDto
//         {
//             Id = x.Id,
//             GroupId = x.GroupId,
//             Comment = x.Comment,
//             StudentId = x.StudentId,
//             Value = x.Value,
//             BonusPoints = x.BonusPoints,
//             LessonId = x.LessonId,
//             WeekIndex = x.WeekIndex
//         }).ToList();
//         
//         return new Response<List<GetGradeDto>>(dto);
//     }
//     #endregion
//     
//     #region GetGradesByGroup
//     public async Task<Response<List<GetGradeDto>>> GetGradesByGroupAsync(int groupId)
//     {
//         var grades = await context.Grades
//             .Include(g => g.Student)
//             .Include(g => g.Group)
//             .Include(g => g.Lesson)
//             .Where(g => g.GroupId == groupId)
//             .ToListAsync();
//             
//         if (grades.Count == 0) 
//             return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "No grades found for this group");
//             
//         var dto = grades.Select(x => new GetGradeDto
//         {
//             Id = x.Id,
//             GroupId = x.GroupId,
//             Comment = x.Comment,
//             StudentId = x.StudentId,
//             Value = x.Value,
//             BonusPoints = x.BonusPoints,
//             LessonId = x.LessonId,
//             WeekIndex = x.WeekIndex
//         }).ToList();
//         
//         return new Response<List<GetGradeDto>>(dto);
//     }
//     #endregion
//     
//     #region GetGradesByLesson
//     public async Task<Response<List<GetGradeDto>>> GetGradesByLessonAsync(int lessonId)
//     {
//         var grades = await context.Grades
//             .Include(g => g.Student)
//             .Include(g => g.Group)
//             .Include(g => g.Lesson)
//             .Where(g => g.LessonId == lessonId)
//             .ToListAsync();
//             
//         if (grades.Count == 0) 
//             return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "No grades found for this lesson");
//             
//         var dto = grades.Select(x => new GetGradeDto
//         {
//             Id = x.Id,
//             GroupId = x.GroupId,
//             Comment = x.Comment,
//             StudentId = x.StudentId,
//             Value = x.Value,
//             BonusPoints = x.BonusPoints,
//             LessonId = x.LessonId,
//             WeekIndex = x.WeekIndex
//         }).ToList();
//         
//         return new Response<List<GetGradeDto>>(dto);
//     }
//     #endregion
//     
//     #region GetGradesByWeek
//     public async Task<Response<List<GetGradeDto>>> GetGradesByWeekAsync(int groupId, int weekIndex)
//     {
//         var grades = await context.Grades
//             .Include(g => g.Student)
//             .Include(g => g.Group)
//             .Include(g => g.Lesson)
//             .Where(g => g.GroupId == groupId && g.WeekIndex == weekIndex)
//             .ToListAsync();
//             
//         if (grades.Count == 0) 
//             return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, $"No grades found for group {groupId} in week {weekIndex}");
//             
//         var dto = grades.Select(x => new GetGradeDto
//         {
//             Id = x.Id,
//             GroupId = x.GroupId,
//             Comment = x.Comment,
//             StudentId = x.StudentId,
//             Value = x.Value,
//             BonusPoints = x.BonusPoints,
//             LessonId = x.LessonId,
//             WeekIndex = x.WeekIndex
//         }).ToList();
//         
//         return new Response<List<GetGradeDto>>(dto);
//     }
//     #endregion
//     
//     #region GetStudentGradesByWeek
//     public async Task<Response<List<GetGradeDto>>> GetStudentGradesByWeekAsync(int studentId, int groupId, int weekIndex)
//     {
//         var grades = await context.Grades
//             .Include(g => g.Student)
//             .Include(g => g.Group)
//             .Include(g => g.Lesson)
//             .Where(g => g.StudentId == studentId && g.GroupId == groupId && g.WeekIndex == weekIndex)
//             .ToListAsync();
//             
//         if (grades.Count == 0) 
//             return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, $"No grades found for student {studentId} in group {groupId}, week {weekIndex}");
//             
//         var dto = grades.Select(x => new GetGradeDto
//         {
//             Id = x.Id,
//             GroupId = x.GroupId,
//             Comment = x.Comment,
//             StudentId = x.StudentId,
//             Value = x.Value,
//             BonusPoints = x.BonusPoints,
//             LessonId = x.LessonId,
//             WeekIndex = x.WeekIndex
//         }).ToList();
//         
//         return new Response<List<GetGradeDto>>(dto);
//     }
//     #endregion
//     
//     #region CreateGrade
//     public async Task<Response<string>> CreateGradeAsync(CreateGradeDto grade)
//     {
//         var student = await context.Students.FirstOrDefaultAsync(x => x.Id == grade.StudentId);
//         if (student == null) 
//             return new Response<string>(HttpStatusCode.NotFound, "Student not found");
//             
//         var lesson = await context.Lessons.FirstOrDefaultAsync(x => x.Id == grade.LessonId);
//         if (lesson == null) 
//             return new Response<string>(HttpStatusCode.NotFound, "Lesson not found");
//             
//         var group = await context.Groups.FirstOrDefaultAsync(x => x.Id == grade.GroupId);
//         if (group == null) 
//             return new Response<string>(HttpStatusCode.NotFound, "Group not found");
//
//         var newGrade = new Grade
//         {
//             StudentId = grade.StudentId,
//             GroupId = grade.GroupId,
//             Comment = grade.Comment,
//             Value = grade.Value,
//             BonusPoints = grade.BonusPoints,
//             LessonId = grade.LessonId,
//             WeekIndex = grade.WeekIndex ?? lesson.WeekIndex,
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow
//         };
//         
//         context.Grades.Add(newGrade);
//         var res = await context.SaveChangesAsync();
//         
//         return res > 0
//             ? new Response<string>(HttpStatusCode.Created, "Grade created successfully")
//             : new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
//     }
//     #endregion
//     
//     #region EditGrade
//     public async Task<Response<string>> EditGradeAsync(UpdateGradeDto grade)
//     {
//         var oldGrade = await context.Grades.FirstOrDefaultAsync(x => x.Id == grade.Id);
//         if (oldGrade == null) 
//             return new Response<string>(HttpStatusCode.NotFound, "Grade not found");
//             
//         oldGrade.Comment = grade.Comment;
//         oldGrade.Value = grade.Value;
//         oldGrade.BonusPoints = grade.BonusPoints;
//         oldGrade.UpdatedAt = DateTime.UtcNow;
//         
//         var res = await context.SaveChangesAsync();
//         
//         return res > 0 
//             ? new Response<string>(HttpStatusCode.OK, "Grade updated successfully")
//             : new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
//     }
//     #endregion
//
//     #region DeleteGrade
//     public async Task<Response<string>> DeleteGradeAsync(int id)
//     {
//         var grade = await context.Grades.FirstOrDefaultAsync(x => x.Id == id);
//         if (grade == null) 
//             return new Response<string>(HttpStatusCode.NotFound, "Grade not found");
//             
//         context.Grades.Remove(grade);
//         var res = await context.SaveChangesAsync();
//         
//         return res > 0 
//             ? new Response<string>(HttpStatusCode.OK, "Grade deleted successfully")
//             : new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
//     }
//     #endregion
//
//     #region GetStudentAverageGradeAsync
//     public async Task<Response<double>> GetStudentAverageGradeAsync(int studentId, int? groupId = null)
//     {
//         try
//         {
//             var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
//             if (student == null)
//                 return new Response<double>(HttpStatusCode.NotFound, "Student not found");
//             
//             // Базовый запрос
//             IQueryable<Grade> gradesQuery = context.Grades
//                 .Where(g => g.StudentId == studentId && !g.IsDeleted && g.Value.HasValue);
//             
//             // Если указан groupId, фильтруем по группе
//             if (groupId.HasValue)
//             {
//                 var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId.Value && !g.IsDeleted);
//                 if (group == null)
//                     return new Response<double>(HttpStatusCode.NotFound, "Group not found");
//                 
//                 gradesQuery = gradesQuery.Where(g => g.GroupId == groupId.Value);
//             }
//             
//             // Получаем все оценки студента
//             var grades = await gradesQuery.ToListAsync();
//             
//             if (grades.Count == 0)
//                 return new Response<double>(0, "Student has no grades");
//             
//             // Рассчитываем среднюю оценку
//             double averageGrade = grades.Average(g => g.Value!.Value);
//             
//             return new Response<double>(averageGrade);
//         }
//         catch (Exception ex)
//         {
//             return new Response<double>(HttpStatusCode.InternalServerError, ex.Message);
//         }
//     }
//     #endregion
//
//     #region GetStudentGradeStatisticsAsync
//     public async Task<Response<Dictionary<string, double>>> GetStudentGradeStatisticsAsync(int studentId, int groupId)
//     {
//         try
//         {
//             var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
//             if (student == null)
//                 return new Response<Dictionary<string, double>>(HttpStatusCode.NotFound, "Student not found");
//             
//             var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
//             if (group == null)
//                 return new Response<Dictionary<string, double>>(HttpStatusCode.NotFound, "Group not found");
//             
//             // Получаем все оценки студента по урокам в этой группе
//             var lessonGrades = await context.Grades
//                 .Where(g => g.StudentId == studentId && g.GroupId == groupId && !g.IsDeleted && g.Value.HasValue)
//                 .ToListAsync();
//             
//             // Получаем все оценки за экзамены студента в этой группе
//             var exams = await context.Exams
//                 .Where(e => e.StudentId == studentId && e.GroupId == groupId && !e.IsDeleted && e.Value.HasValue)
//                 .ToListAsync();
//             
//             var statistics = new Dictionary<string, double>();
//             
//             // Средняя оценка за уроки
//             if (lessonGrades.Any())
//                 statistics["AverageLessonGrade"] = lessonGrades.Average(g => g.Value!.Value);
//             else
//                 statistics["AverageLessonGrade"] = 0;
//             
//             // Средняя оценка за экзамены
//             if (exams.Any())
//                 statistics["AverageExamGrade"] = exams.Average(e => e.Value!.Value);
//             else
//                 statistics["AverageExamGrade"] = 0;
//             
//             // Средний бонусный балл за уроки
//             if (lessonGrades.Any(g => g.BonusPoints.HasValue))
//                 statistics["AverageLessonBonus"] = lessonGrades
//                     .Where(g => g.BonusPoints.HasValue)
//                     .Average(g => g.BonusPoints!.Value);
//             else
//                 statistics["AverageLessonBonus"] = 0;
//             
//             // Средний бонусный балл за экзамены
//             if (exams.Any(e => e.BonusPoints.HasValue))
//                 statistics["AverageExamBonus"] = exams
//                     .Where(e => e.BonusPoints.HasValue)
//                     .Average(e => e.BonusPoints!.Value);
//             else
//                 statistics["AverageExamBonus"] = 0;
//             
//             // Общая средняя оценка (уроки + экзамены)
//             double totalGradesSum = 0;
//             int totalGradesCount = 0;
//             
//             if (lessonGrades.Any())
//             {
//                 totalGradesSum += lessonGrades.Sum(g => g.Value!.Value);
//                 totalGradesCount += lessonGrades.Count;
//             }
//             
//             if (exams.Any())
//             {
//                 totalGradesSum += exams.Sum(e => e.Value!.Value);
//                 totalGradesCount += exams.Count;
//             }
//             
//             statistics["TotalAverageGrade"] = totalGradesCount > 0 ? totalGradesSum / totalGradesCount : 0;
//             
//             // Общий средний бонусный балл
//             double totalBonusSum = 0;
//             int totalBonusCount = 0;
//             
//             if (lessonGrades.Any(g => g.BonusPoints.HasValue))
//             {
//                 totalBonusSum += lessonGrades.Where(g => g.BonusPoints.HasValue).Sum(g => g.BonusPoints!.Value);
//                 totalBonusCount += lessonGrades.Count(g => g.BonusPoints.HasValue);
//             }
//             
//             if (exams.Any(e => e.BonusPoints.HasValue))
//             {
//                 totalBonusSum += exams.Where(e => e.BonusPoints.HasValue).Sum(e => e.BonusPoints!.Value);
//                 totalBonusCount += exams.Count(e => e.BonusPoints.HasValue);
//             }
//             
//             statistics["TotalAverageBonus"] = totalBonusCount > 0 ? totalBonusSum / totalBonusCount : 0;
//             
//             return new Response<Dictionary<string, double>>(statistics);
//         }
//         catch (Exception ex)
//         {
//             return new Response<Dictionary<string, double>>(HttpStatusCode.InternalServerError, ex.Message);
//         }
//     }
//     #endregion
// }