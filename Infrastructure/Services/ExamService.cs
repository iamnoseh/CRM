using System.Net;
using Domain.DTOs.Exam;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ExamService(DataContext context) : IExamService
{
    #region Exam Methods

    public async Task<Response<GetExamDto>> GetExamById(int id)
    {
        try
        {
            var exam = await context.Exams
                .Include(x => x.Group)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (exam == null)
                return new Response<GetExamDto>(HttpStatusCode.NotFound, "Экзамен не найден");

            var dto = new GetExamDto
            {
                Id = exam.Id,
                ExamDate = exam.ExamDate,
                GroupId = exam.GroupId,
                WeekIndex = exam.WeekIndex,
                MaxPoints = exam.MaxPoints
            };


            return new Response<GetExamDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetExamDto>(HttpStatusCode.InternalServerError,
                $"Ошибка при получении экзамена: {ex.Message}");
        }
    }

    public async Task<Response<List<GetExamDto>>> GetExams()
    {
        try
        {
            var exams = await context.Exams
                .Include(x => x.Group)
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.ExamDate)
                .ToListAsync();

            if (exams.Count == 0)
                return new Response<List<GetExamDto>>(HttpStatusCode.NotFound, "Экзамены не найдены");

            var dtos = exams.Select(exam => new GetExamDto
            {
                Id = exam.Id,
                ExamDate = exam.ExamDate,
                GroupId = exam.GroupId,
                WeekIndex = exam.WeekIndex,
                MaxPoints = exam.MaxPoints
            }).ToList();

            return new Response<List<GetExamDto>>(dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GetExamDto>>(HttpStatusCode.InternalServerError,
                $"Ошибка при получении экзаменов: {ex.Message}");
        }
    }

    public async Task<Response<List<GetExamDto>>> GetExamsByGroup(int groupId)
    {
        try
        {
            var group = await context.Groups.FirstOrDefaultAsync(x => x.Id == groupId && !x.IsDeleted);
            if (group == null)
                return new Response<List<GetExamDto>>(HttpStatusCode.NotFound, "Группа не найдена");

            var exams = await context.Exams
                .Include(x => x.Group)
                .Where(x => x.GroupId == groupId && !x.IsDeleted)
                .OrderByDescending(x => x.ExamDate)
                .ToListAsync();

            if (exams.Count == 0)
                return new Response<List<GetExamDto>>(HttpStatusCode.NotFound, "Экзамены для данной группы не найдены");

            var dtos = exams.Select(exam => new GetExamDto
            {
                Id = exam.Id,
                ExamDate = exam.ExamDate,
                GroupId = exam.GroupId,
                WeekIndex = exam.WeekIndex,
                MaxPoints = exam.MaxPoints
            }).ToList();

            return new Response<List<GetExamDto>>(dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GetExamDto>>(HttpStatusCode.InternalServerError,
                $"Ошибка при получении экзаменов для группы: {ex.Message}");
        }
    }

    public async Task<Response<string>> CreateExam(CreateExamDto examDto)
    {
        try
        {
            var group = await context.Groups.FirstOrDefaultAsync(x => x.Id == examDto.GroupId && !x.IsDeleted);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Группа не найдена");

            var existingExam = await context.Exams
                .FirstOrDefaultAsync(x =>
                    x.GroupId == examDto.GroupId &&
                    x.WeekIndex == examDto.WeekIndex &&
                    !x.IsDeleted);

            if (existingExam != null)
                return new Response<string>(HttpStatusCode.BadRequest,
                    "Экзамен для данной группы на указанной неделе уже существует");

            var newExam = new Exam
            {
                GroupId = examDto.GroupId,
                WeekIndex = examDto.WeekIndex,
                ExamDate = examDto.ExamDate,
                MaxPoints = examDto.MaxPoints,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.Exams.AddAsync(newExam);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.Created, "Экзамен успешно создан")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось создать экзамен");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                $"Ошибка при создании экзамена: {ex.Message}");
        }
    }

    public async Task<Response<string>> UpdateExam(int id, UpdateExamDto examDto)
    {
        try
        {
            var exam = await context.Exams.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (exam == null)
                return new Response<string>(HttpStatusCode.NotFound, "Экзамен не найден");
            
            if (examDto.ExamDate.HasValue)
                exam.ExamDate = examDto.ExamDate.Value;

            if (examDto.WeekIndex.HasValue)
                exam.WeekIndex = examDto.WeekIndex.Value;

            if (examDto.MaxPoints.HasValue)
                exam.MaxPoints = examDto.MaxPoints.Value;

            exam.UpdatedAt = DateTime.UtcNow;

            context.Exams.Update(exam);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Экзамен успешно обновлен")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось обновить экзамен");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                $"Ошибка при обновлении экзамена: {ex.Message}");
        }
    }

    public async Task<Response<string>> DeleteExam(int id)
    {
        try
        {
            var exam = await context.Exams.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (exam == null)
                return new Response<string>(HttpStatusCode.NotFound, "Экзамен не найден");

            exam.IsDeleted = true;
            exam.UpdatedAt = DateTime.UtcNow;
            
            var examGrades = await context.ExamGrades
                .Where(g => g.ExamId == id && !g.IsDeleted)
                .ToListAsync();

            foreach (var grade in examGrades)
            {
                grade.IsDeleted = true;
                grade.UpdatedAt = DateTime.UtcNow;
            }

            context.Exams.Update(exam);
            if (examGrades.Any())
                context.ExamGrades.UpdateRange(examGrades);

            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Экзамен и все связанные оценки успешно удалены")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось удалить экзамен");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                $"Ошибка при удалении экзамена: {ex.Message}");
        }
    }

    #endregion

    #region Exam Grade Methods

    public async Task<Response<GetExamGradeDto>> GetExamGradeById(int id)
    {
        try
        {
            var grade = await context.ExamGrades
                .Include(x => x.Student)
                .Include(x => x.Exam)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (grade == null)
                return new Response<GetExamGradeDto>(HttpStatusCode.NotFound, "Оценка за экзамен не найдена");

            var dto = new GetExamGradeDto
            {
                Id = grade.Id,
                ExamId = grade.ExamId,
                StudentId = grade.StudentId,
                StudentName = grade.Student.FullName,
                Points = grade.Points,
                HasPassed = grade.HasPassed,
                Comment = grade.Comment,
                BonusPoints = grade.BonusPoint
            };

            return new Response<GetExamGradeDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetExamGradeDto>(HttpStatusCode.InternalServerError,
                $"Ошибка при получении оценки за экзамен: {ex.Message}");
        }
    }

    public async Task<Response<List<GetExamGradeDto>>> GetExamGradesByExam(int examId)
    {
        try
        {
            var exam = await context.Exams.FirstOrDefaultAsync(x => x.Id == examId && !x.IsDeleted);
            if (exam == null)
                return new Response<List<GetExamGradeDto>>(HttpStatusCode.NotFound, "Экзамен не найден");

            var grades = await context.ExamGrades
                .Include(x => x.Student)
                .Include(x => x.Exam)
                .Where(x => x.ExamId == examId && !x.IsDeleted)
                .ToListAsync();

            if (grades.Count == 0)
                return new Response<List<GetExamGradeDto>>(HttpStatusCode.NotFound,
                    "Оценки за данный экзамен не найдены");

            var dtos = grades.Select(grade => new GetExamGradeDto
            {
                Id = grade.Id,
                ExamId = grade.ExamId,
                StudentId = grade.StudentId,
                StudentName = grade.Student.FullName,
                Points = grade.Points,
                HasPassed = grade.HasPassed,
                Comment = grade.Comment,
                BonusPoints = grade.BonusPoint
            }).ToList();

            return new Response<List<GetExamGradeDto>>(dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GetExamGradeDto>>(HttpStatusCode.InternalServerError,
                $"Ошибка при получении оценок за экзамен: {ex.Message}");
        }
    }

    public async Task<Response<List<GetExamGradeDto>>> GetExamGradesByStudent(int studentId)
    {
        try
        {
            var student = await context.Students.FirstOrDefaultAsync(x => x.Id == studentId && !x.IsDeleted);
            if (student == null)
                return new Response<List<GetExamGradeDto>>(HttpStatusCode.NotFound, "Студент не найден");

            var grades = await context.ExamGrades
                .Include(x => x.Student)
                .Include(x => x.Exam)
                .Where(x => x.StudentId == studentId && !x.IsDeleted)
                .ToListAsync();

            if (grades.Count == 0)
                return new Response<List<GetExamGradeDto>>(HttpStatusCode.NotFound,
                    "Оценки за экзамены для данного студента не найдены");

            var dtos = grades.Select(grade => new GetExamGradeDto
            {
                Id = grade.Id,
                ExamId = grade.ExamId,
                StudentId = grade.StudentId,
                StudentName = grade.Student.FullName,
                Points = grade.Points,
                HasPassed = grade.HasPassed,
                Comment = grade.Comment,
                BonusPoints = grade.BonusPoint
            }).ToList();

            return new Response<List<GetExamGradeDto>>(dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GetExamGradeDto>>(HttpStatusCode.InternalServerError,
                $"Ошибка при получении оценок за экзамены студента: {ex.Message}");
        }
    }

    public async Task<Response<string>> CreateExamGrade(CreateExamGradeDto gradeDto)
    {
        try
        {
            var exam = await context.Exams.FirstOrDefaultAsync(x => x.Id == gradeDto.ExamId && !x.IsDeleted);
            if (exam == null)
                return new Response<string>(HttpStatusCode.NotFound, "Экзамен не найден");

            var student = await context.Students.FirstOrDefaultAsync(x => x.Id == gradeDto.StudentId && !x.IsDeleted);
            if (student == null)
                return new Response<string>(HttpStatusCode.NotFound, "Студент не найден");

            var existingGrade = await context.ExamGrades
                .FirstOrDefaultAsync(x =>
                    x.ExamId == gradeDto.ExamId &&
                    x.StudentId == gradeDto.StudentId &&
                    !x.IsDeleted);

            if (existingGrade != null)
                return new Response<string>(HttpStatusCode.BadRequest,
                    "Оценка для этого студента за данный экзамен уже существует");

            if (gradeDto.Points > exam.MaxPoints)
                return new Response<string>(HttpStatusCode.BadRequest,
                    $"Количество баллов не может превышать максимум для экзамена ({exam.MaxPoints})");

            var newGrade = new ExamGrade
            {
                ExamId = gradeDto.ExamId,
                StudentId = gradeDto.StudentId,
                Points = gradeDto.Points,
                BonusPoint = gradeDto.BonusPoints,
                Comment = gradeDto.Comment,
                HasPassed = gradeDto.HasPassed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.ExamGrades.AddAsync(newGrade);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.Created, "Оценка за экзамен успешно создана")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось создать оценку за экзамен");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                $"Ошибка при создании оценки за экзамен: {ex.Message}");
        }
    }

    public async Task<Response<string>> UpdateExamGrade(int id, UpdateExamGradeDto gradeDto)
    {
        try
        {
            var grade = await context.ExamGrades
                .Include(x => x.Exam)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (grade == null)
                return new Response<string>(HttpStatusCode.NotFound, "Оценка за экзамен не найдена");

            if (gradeDto.Points.HasValue && gradeDto.Points.Value > grade.Exam.MaxPoints)
                return new Response<string>(HttpStatusCode.BadRequest,
                    $"Количество баллов не может превышать максимум для экзамена ({grade.Exam.MaxPoints})");

            if (gradeDto.Points.HasValue)
                grade.Points = gradeDto.Points.Value;

            if (gradeDto.HasPassed.HasValue)
                grade.HasPassed = gradeDto.HasPassed.Value;

            if (!string.IsNullOrEmpty(gradeDto.Comment))
                grade.Comment = gradeDto.Comment;

            if (gradeDto.BonusPoints.HasValue)
                grade.BonusPoint = gradeDto.BonusPoints.Value;

            grade.UpdatedAt = DateTime.UtcNow;

            context.ExamGrades.Update(grade);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Оценка за экзамен успешно обновлена")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось обновить оценку за экзамен");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                $"Ошибка при обновлении оценки за экзамен: {ex.Message}");
        }
    }

    public async Task<Response<string>> DeleteExamGrade(int id)
    {
        try
        {
            var grade = await context.ExamGrades.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (grade == null)
                return new Response<string>(HttpStatusCode.NotFound, "Оценка за экзамен не найдена");

            grade.IsDeleted = true;
            grade.UpdatedAt = DateTime.UtcNow;

            context.ExamGrades.Update(grade);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Оценка за экзамен успешно удалена")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось удалить оценку за экзамен");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                $"Ошибка при удалении оценки за экзамен: {ex.Message}");
        }
    }

    public async Task<Response<string>> AddBonusPoint(int examGradeId, int bonusPoints)
    {
        try
        {
            var grade = await context.ExamGrades.FirstOrDefaultAsync(x => x.Id == examGradeId && !x.IsDeleted);
            if (grade == null)
                return new Response<string>(HttpStatusCode.NotFound, "Оценка за экзамен не найдена");

            grade.BonusPoint = grade.BonusPoint + bonusPoints;
            grade.UpdatedAt = DateTime.UtcNow;

            context.ExamGrades.Update(grade);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK,
                    $"Бонусные баллы ({bonusPoints}) успешно добавлены. Всего бонусных баллов: {grade.BonusPoint}")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось добавить бонусные баллы");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                $"Ошибка при добавлении бонусных баллов: {ex.Message}");
        }
    }

    #endregion
}
