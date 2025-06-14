using System.Net;
using Domain.DTOs.Exam;
using Domain.Entities;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ExamService(DataContext context) : IExamService
{
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
            
            // Получаем и отмечаем как удаленные все оценки, связанные с этим экзаменом
            var grades = await context.Grades
                .Where(g => g.ExamId == id && !g.IsDeleted)
                .ToListAsync();

            foreach (var grade in grades)
            {
                grade.IsDeleted = true;
                grade.UpdatedAt = DateTime.UtcNow;
            }

            context.Exams.Update(exam);
            if (grades.Any())
                context.Grades.UpdateRange(grades);

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
}
