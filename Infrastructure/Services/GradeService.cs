using System.Net;
using Domain.DTOs.Grade;
using Domain.Entities;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class GradeService(DataContext context) : IGradeService
{
    #region GetGradeByIdAsync

    public async Task<Response<GetGradeDto>> GetGradeByIdAsync(int id)
    {
        try
        {
            var grade = await context.Grades
                .Include(g => g.Student)
                .Include(g => g.Group)
                .Include(g => g.Lesson)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (grade == null)
                return new Response<GetGradeDto>(HttpStatusCode.NotFound, "Grade not found");

            var dto = new GetGradeDto
            {
                Id = grade.Id,
                GroupId = grade.GroupId,
                Comment = grade.Comment,
                StudentId = grade.StudentId,
                StudentName = grade.Student?.FullName,
                Value = grade.Value,
                BonusPoints = grade.BonusPoints,
                LessonId = grade.LessonId,
                ExamId = grade.ExamId,
                WeekIndex = grade.WeekIndex,
                DayIndex = grade.DayIndex,
                CreatedAt = grade.CreatedAt
                // GradeType вычисляется автоматически через свойство в DTO
            };

            return new Response<GetGradeDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetGradeDto>(HttpStatusCode.InternalServerError, $"Error getting grade: {ex.Message}");
        }
    }

    #endregion

    #region GetGrades

    public async Task<Response<List<GetGradeDto>>> GetAllGradesAsync()
    {
        try
        {
            var grades = await context.Grades
                .Include(g => g.Student)
                .Include(g => g.Group)
                .Include(g => g.Lesson)
                .Where(g => !g.IsDeleted)
                .ToListAsync();

            if (grades.Count == 0)
                return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "No Grades found");

            var dto = grades.Select(x => new GetGradeDto
            {
                Id = x.Id,
                GroupId = x.GroupId,
                Comment = x.Comment,
                StudentId = x.StudentId,
                StudentName = x.Student?.FullName,
                Value = x.Value,
                BonusPoints = x.BonusPoints,
                LessonId = x.LessonId,
                ExamId = x.ExamId,
                WeekIndex = x.WeekIndex,
                DayIndex = x.DayIndex,
                CreatedAt = x.CreatedAt
                // GradeType вычисляется автоматически через свойство в DTO
            }).ToList();

            return new Response<List<GetGradeDto>>(dto);
        }
        catch (Exception ex)
        {
            return new Response<List<GetGradeDto>>(HttpStatusCode.InternalServerError,
                $"Error getting grades: {ex.Message}");
        }
    }

    #endregion

    #region GetGradesByStudentId

    public async Task<Response<List<GetGradeDto>>> GetGradesByStudentAsync(int studentId)
    {
        try
        {
            var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student == null)
                return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "Student not found");

            var grades = await context.Grades
                .Include(g => g.Student)
                .Include(g => g.Group)
                .Include(g => g.Lesson)
                .Where(g => g.StudentId == studentId && !g.IsDeleted)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            if (grades.Count == 0)
                return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "No grades found for this student");

            var dto = grades.Select(x => new GetGradeDto
            {
                Id = x.Id,
                GroupId = x.GroupId,
                Comment = x.Comment,
                StudentId = x.StudentId,
                StudentName = x.Student?.FullName,
                Value = x.Value,
                BonusPoints = x.BonusPoints,
                LessonId = x.LessonId,
                ExamId = x.ExamId,
                WeekIndex = x.WeekIndex,
                DayIndex = x.DayIndex,
                CreatedAt = x.CreatedAt
                // GradeType вычисляется автоматически через свойство в DTO
            }).ToList();

            return new Response<List<GetGradeDto>>(dto);
        }
        catch (Exception ex)
        {
            return new Response<List<GetGradeDto>>(HttpStatusCode.InternalServerError,
                $"Error getting student grades: {ex.Message}");
        }
    }

    #endregion

    #region GetGradesByGroup

    public async Task<Response<List<GetGradeDto>>> GetGradesByGroupAsync(int groupId)
    {
        try
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null)
                return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "Group not found");

            var grades = await context.Grades
                .Include(g => g.Student)
                .Include(g => g.Group)
                .Include(g => g.Lesson)
                .Where(g => g.GroupId == groupId && !g.IsDeleted)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            if (grades.Count == 0)
                return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "No grades found for this group");

            var dto = grades.Select(x => new GetGradeDto
            {
                Id = x.Id,
                GroupId = x.GroupId,
                Comment = x.Comment,
                StudentId = x.StudentId,
                StudentName = x.Student?.FullName,
                Value = x.Value,
                BonusPoints = x.BonusPoints,
                LessonId = x.LessonId,
                ExamId = x.ExamId,
                WeekIndex = x.WeekIndex,
                DayIndex = x.DayIndex,
                CreatedAt = x.CreatedAt
                // GradeType вычисляется автоматически через свойство в DTO
            }).ToList();

            return new Response<List<GetGradeDto>>(dto);
        }
        catch (Exception ex)
        {
            return new Response<List<GetGradeDto>>(HttpStatusCode.InternalServerError,
                $"Error getting group grades: {ex.Message}");
        }
    }

    #endregion

    #region GetGradesByLesson

    public async Task<Response<List<GetGradeDto>>> GetGradesByLessonAsync(int lessonId)
    {
        try
        {
            var lesson = await context.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted);
            if (lesson == null)
                return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "Lesson not found");

            var grades = await context.Grades
                .Include(g => g.Student)
                .Include(g => g.Group)
                .Include(g => g.Lesson)
                .Where(g => g.LessonId == lessonId && !g.IsDeleted)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            if (grades.Count == 0)
                return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "No grades found for this lesson");

            var dto = grades.Select(x => new GetGradeDto
            {
                Id = x.Id,
                GroupId = x.GroupId,
                Comment = x.Comment,
                StudentId = x.StudentId,
                StudentName = x.Student?.FullName,
                Value = x.Value,
                BonusPoints = x.BonusPoints,
                LessonId = x.LessonId,
                ExamId = x.ExamId,
                WeekIndex = x.WeekIndex,
                DayIndex = x.DayIndex,
                CreatedAt = x.CreatedAt
                // GradeType вычисляется автоматически через свойство в DTO
            }).ToList();

            return new Response<List<GetGradeDto>>(dto);
        }
        catch (Exception ex)
        {
            return new Response<List<GetGradeDto>>(HttpStatusCode.InternalServerError,
                $"Error getting lesson grades: {ex.Message}");
        }
    }

    #endregion

    #region CreateGrade

    public async Task<Response<string>> CreateGradeAsync(CreateGradeDto grade)
    {
        try
        {
            var student = await context.Students.FirstOrDefaultAsync(x => x.Id == grade.StudentId && !x.IsDeleted);
            if (student == null)
                return new Response<string>(HttpStatusCode.NotFound, "Student not found");

            var lesson = await context.Lessons.FirstOrDefaultAsync(x => x.Id == grade.LessonId && !x.IsDeleted);
            if (lesson == null)
                return new Response<string>(HttpStatusCode.NotFound, "Lesson not found");

            var group = await context.Groups.FirstOrDefaultAsync(x => x.Id == grade.GroupId && !x.IsDeleted);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Group not found");

            var existingGrade = await context.Grades
                .FirstOrDefaultAsync(g => g.StudentId == grade.StudentId &&
                                          g.LessonId == grade.LessonId &&
                                          !g.IsDeleted);

            if (existingGrade != null)
                return new Response<string>(HttpStatusCode.BadRequest,
                    "Grade already exists for this student and lesson");

            var newGrade = new Grade
            {
                StudentId = grade.StudentId,
                GroupId = grade.GroupId,
                Comment = grade.Comment,
                Value = grade.Value,
                BonusPoints = grade.BonusPoints,
                LessonId = grade.LessonId,
                WeekIndex = grade.WeekIndex ?? lesson.WeekIndex,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Grades.Add(newGrade);
            var res = await context.SaveChangesAsync();

            return res > 0
                ? new Response<string>(HttpStatusCode.Created, "Grade created successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Error creating grade: {ex.Message}");
        }
    }

    #endregion

    #region AddBonusPoint

    public async Task<Response<string>> AddBonusPoint(int studentId, int lessonId)
    {
        try
        {
            var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student == null)
                return new Response<string>(HttpStatusCode.NotFound, "Student not found");

            var lesson = await context.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted);
            if (lesson == null)
                return new Response<string>(HttpStatusCode.NotFound, "Lesson not found");

            var existingGrade = await context.Grades
                .FirstOrDefaultAsync(g => g.StudentId == studentId && g.LessonId == lessonId && !g.IsDeleted);

            if (existingGrade != null)
            {
                existingGrade.BonusPoints = (existingGrade.BonusPoints ?? 0) + 1;
                existingGrade.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newGrade = new Grade
                {
                    StudentId = studentId,
                    GroupId = lesson.GroupId,
                    LessonId = lessonId,
                    BonusPoints = 1, 
                    WeekIndex = lesson.WeekIndex,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Grades.Add(newGrade);
            }

            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Bonus point added successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to add bonus point");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Error adding bonus point: {ex.Message}");
        }
    }

    #endregion

    #region GetGradesByWeek

    public async Task<Response<List<GetGradeDto>>> GetGradesByWeekAsync(int groupId, int weekIndex)
    {
        try
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null)
                return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "Group not found");

            var grades = await context.Grades
                .Include(g => g.Student)
                .Include(g => g.Group)
                .Include(g => g.Lesson)
                .Where(g => g.GroupId == groupId && g.WeekIndex == weekIndex && !g.IsDeleted)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            if (grades.Count == 0)
                return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound,
                    $"No grades found for group {groupId} in week {weekIndex}");

            var dto = grades.Select(x => new GetGradeDto
            {
                Id = x.Id,
                GroupId = x.GroupId,
                Comment = x.Comment,
                StudentId = x.StudentId,
                StudentName = x.Student?.FullName,
                Value = x.Value,
                BonusPoints = x.BonusPoints,
                LessonId = x.LessonId,
                ExamId = x.ExamId,
                WeekIndex = x.WeekIndex,
                DayIndex = x.DayIndex,
                CreatedAt = x.CreatedAt
                // GradeType вычисляется автоматически через свойство в DTO
            }).ToList();

            return new Response<List<GetGradeDto>>(dto);
        }
        catch (Exception ex)
        {
            return new Response<List<GetGradeDto>>(HttpStatusCode.InternalServerError,
                $"Error getting grades by week: {ex.Message}");
        }
    }

    #endregion

    #region GetStudentGradesByWeek

    public async Task<Response<List<GetGradeDto>>> GetStudentGradesByWeekAsync(int studentId, int groupId,
        int weekIndex)
    {
        try
        {
            var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student == null)
                return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "Student not found");

            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null)
                return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "Group not found");

            var grades = await context.Grades
                .Include(g => g.Student)
                .Include(g => g.Group)
                .Include(g => g.Lesson)
                .Where(g => g.StudentId == studentId && g.GroupId == groupId && g.WeekIndex == weekIndex &&
                            !g.IsDeleted)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            if (grades.Count == 0)
                return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound,
                    $"No grades found for student {studentId} in group {groupId}, week {weekIndex}");

            var dto = grades.Select(x => new GetGradeDto
            {
                Id = x.Id,
                GroupId = x.GroupId,
                Comment = x.Comment,
                StudentId = x.StudentId,
                StudentName = x.Student?.FullName,
                Value = x.Value,
                BonusPoints = x.BonusPoints,
                LessonId = x.LessonId,
                ExamId = x.ExamId,
                WeekIndex = x.WeekIndex,
                DayIndex = x.DayIndex,
                CreatedAt = x.CreatedAt
                // GradeType вычисляется автоматически через свойство в DTO
            }).ToList();

            return new Response<List<GetGradeDto>>(dto);
        }
        catch (Exception ex)
        {
            return new Response<List<GetGradeDto>>(HttpStatusCode.InternalServerError,
                $"Error getting student grades by week: {ex.Message}");
        }
    }

    #endregion

    #region EditGrade

    public async Task<Response<string>> EditGradeAsync(UpdateGradeDto grade)
    {
        try
        {
            var oldGrade = await context.Grades.FirstOrDefaultAsync(x => x.Id == grade.Id && !x.IsDeleted);
            if (oldGrade == null)
                return new Response<string>(HttpStatusCode.NotFound, "Grade not found");

            oldGrade.Comment = grade.Comment;
            oldGrade.Value = grade.Value;
            oldGrade.BonusPoints = grade.BonusPoints;
            oldGrade.UpdatedAt = DateTime.UtcNow;

            var res = await context.SaveChangesAsync();

            return res > 0
                ? new Response<string>(HttpStatusCode.OK, "Grade updated successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Error updating grade: {ex.Message}");
        }
    }

    #endregion

    #region DeleteGrade

    public async Task<Response<string>> DeleteGradeAsync(int id)
    {
        try
        {
            var grade = await context.Grades.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (grade == null)
                return new Response<string>(HttpStatusCode.NotFound, "Grade not found");

            grade.IsDeleted = true;
            grade.UpdatedAt = DateTime.UtcNow;

            var res = await context.SaveChangesAsync();

            return res > 0
                ? new Response<string>(HttpStatusCode.OK, "Grade deleted successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Error deleting grade: {ex.Message}");
        }
    }

    #endregion

    #region GetStudentAverageGradeAsync

    public async Task<Response<double>> GetStudentAverageGradeAsync(int studentId, int? groupId = null)
    {
        try
        {
            var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student == null)
                return new Response<double>(HttpStatusCode.NotFound, "Student not found");

            IQueryable<Grade> gradesQuery = context.Grades
                .Where(g => g.StudentId == studentId && !g.IsDeleted && g.Value.HasValue);

            if (groupId.HasValue)
            {
                var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId.Value && !g.IsDeleted);
                if (group == null)
                    return new Response<double>(HttpStatusCode.NotFound, "Group not found");

                gradesQuery = gradesQuery.Where(g => g.GroupId == groupId.Value);
            }
            
            var grades = await gradesQuery.ToListAsync();

            if (!grades.Any())
                return new Response<double>(HttpStatusCode.NotFound, "No grades found for this student");

            double totalGradePoints = 0;
            int gradeCount = 0;

            foreach (var grade in grades)
            {
                if (grade.Value.HasValue)
                {
                    totalGradePoints += grade.Value.Value;
                    gradeCount++;
                }

                if (grade.BonusPoints.HasValue && grade.BonusPoints.Value > 0)
                {
                    totalGradePoints += grade.BonusPoints.Value;
                }
            }

            double averageGrade = (gradeCount > 0) ? Math.Round(totalGradePoints / gradeCount, 2) : 0;
            return new Response<double>(averageGrade);
        }
        catch (Exception ex)
        {
            return new Response<double>(HttpStatusCode.InternalServerError,
                $"Error calculating average grade: {ex.Message}");
        }
    }

    #endregion
    
    #region GetGradesByExamAsync
    
    /// <summary>
    /// Возвращает все оценки за определенный экзамен
    /// </summary>
    public async Task<Response<List<GetGradeDto>>> GetGradesByExamAsync(int examId)
    {
        try
        {
            var exam = await context.Exams
                .FirstOrDefaultAsync(e => e.Id == examId && !e.IsDeleted);
                
            if (exam == null)
                return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "Экзамен не найден");
                
            var grades = await context.Grades
                .Include(g => g.Student)
                .Include(g => g.Exam)
                .Where(g => g.ExamId == examId && !g.IsDeleted)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();
            
            if (grades.Count == 0)
                return new Response<List<GetGradeDto>>(HttpStatusCode.NotFound, "Оценки за этот экзамен не найдены");

            var dto = grades.Select(x => new GetGradeDto
            {
                Id = x.Id,
                GroupId = x.GroupId,
                StudentId = x.StudentId,
                ExamId = x.ExamId,
                Value = x.Value,
                BonusPoints = x.BonusPoints,
                Comment = x.Comment,
                WeekIndex = x.WeekIndex,
                DayIndex = x.DayIndex,
                CreatedAt = x.CreatedAt,
                StudentName = x.Student.User.FullName
            }).ToList();

            return new Response<List<GetGradeDto>>(dto);
        }
        catch (Exception ex)
        {
            return new Response<List<GetGradeDto>>(HttpStatusCode.InternalServerError, 
                $"Ошибка при получении оценок за экзамен: {ex.Message}");
        }
    }
    
    #endregion
    
    #region CreateExamGradeAsync
    
    /// <summary>
    /// Создает оценку за экзамен
    /// </summary>
    public async Task<Response<string>> CreateExamGradeAsync(CreateGradeDto gradeDto)
    {
        try
        {
            var exam = await context.Exams
                .FirstOrDefaultAsync(e => e.Id == gradeDto.ExamId && !e.IsDeleted);
                
            if (exam == null)
                return new Response<string>(HttpStatusCode.NotFound, "Экзамен не найден");
                
            var student = await context.Students
                .FirstOrDefaultAsync(s => s.Id == gradeDto.StudentId && !s.IsDeleted);
                
            if (student == null)
                return new Response<string>(HttpStatusCode.NotFound, "Студент не найден");
                
            var existingGrade = await context.Grades
                .FirstOrDefaultAsync(g => g.ExamId == gradeDto.ExamId && 
                                     g.StudentId == gradeDto.StudentId && 
                                     !g.IsDeleted);
                
            if (existingGrade != null)
                return new Response<string>(HttpStatusCode.BadRequest, "Оценка за этот экзамен уже существует для данного студента");
                
            var grade = new Grade
            {
                GroupId = gradeDto.GroupId,
                StudentId = gradeDto.StudentId,
                ExamId = gradeDto.ExamId,
                LessonId = null, // Это оценка за экзамен, а не за урок
                Value = gradeDto.Value,
                Comment = gradeDto.Comment,
                BonusPoints = gradeDto.BonusPoints,
                WeekIndex = gradeDto.WeekIndex,
                DayIndex = gradeDto.DayIndex,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await context.Grades.AddAsync(grade);
            var result = await context.SaveChangesAsync();
            
            return result > 0
                ? new Response<string>(HttpStatusCode.Created, "Оценка за экзамен успешно добавлена")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось добавить оценку за экзамен");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Ошибка при создании оценки за экзамен: {ex.Message}");
        }
    }
    
    #endregion
    
    #region GetStudentExamAverageAsync
    
    /// <summary>
    /// Получает среднюю оценку студента за экзамены
    /// </summary>
    public async Task<Response<double>> GetStudentExamAverageAsync(int studentId, int? groupId = null)
    {
        try
        {
            var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student == null)
                return new Response<double>(HttpStatusCode.NotFound, "Студент не найден");

            var gradesQuery = context.Grades
                .Where(g => g.StudentId == studentId && g.ExamId != null && !g.IsDeleted);

            if (groupId.HasValue)
                gradesQuery = gradesQuery.Where(g => g.GroupId == groupId.Value);

            var grades = await gradesQuery.ToListAsync();

            if (grades.Count == 0)
                return new Response<double>(0);

            var average = grades.Where(g => g.Value.HasValue).Average(g => g.Value.Value);
            return new Response<double>(average);
        }
        catch (Exception ex)
        {
            return new Response<double>(HttpStatusCode.InternalServerError, 
                $"Ошибка при расчете средней оценки за экзамены: {ex.Message}");
        }
    }
    
    #endregion
}
