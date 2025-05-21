using System.Net;
using Domain.DTOs.Lesson;
using Domain.Entities;
using Domain.Enums;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class LessonService(DataContext context) : ILessonService
{
    #region GetLessons
    public async Task<Response<List<GetLessonDto>>> GetLessons()
    {
        try
        {
            var lessons = await context.Lessons
                .Include(l => l.Group)
                .Where(l => !l.IsDeleted)
                .ToListAsync();
                
            if (lessons.Count == 0) 
                return new Response<List<GetLessonDto>>(HttpStatusCode.NotFound, "Lessons not found");
                
            var dto = lessons.Select(x => new GetLessonDto
            {
                Id = x.Id,
                GroupId = x.GroupId,
                StartTime = x.StartTime,
                WeekIndex = x.WeekIndex,
                DayOfWeekIndex = x.DayOfWeekIndex,
                DayIndex = x.DayIndex,
                GroupName = x.Group?.Name ?? "Unknown"
            }).ToList();
            
            return new Response<List<GetLessonDto>>(dto);
        }
        catch (Exception ex)
        {
            return new Response<List<GetLessonDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetLessonById
    public async Task<Response<GetLessonDto>> GetLessonById(int id)
    {
        try
        {
            var lesson = await context.Lessons
                .Include(l => l.Group)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                
            if (lesson == null) 
                return new Response<GetLessonDto>(HttpStatusCode.NotFound, "Lesson not found");
                
            var dto = new GetLessonDto
            {
                Id = lesson.Id,
                GroupId = lesson.GroupId,
                StartTime = lesson.StartTime,
                WeekIndex = lesson.WeekIndex,
                DayOfWeekIndex = lesson.DayOfWeekIndex,
                DayIndex = lesson.DayIndex,
                GroupName = lesson.Group?.Name ?? "Unknown"
            };
            
            return new Response<GetLessonDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetLessonDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region CreateLesson
    public async Task<Response<string>> CreateLesson(CreateLessonDto createLessonDto)
    {
        try
        {
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == createLessonDto.GroupId && !g.IsDeleted);
                
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Group not found");

            var existingLesson = await context.Lessons
                .AnyAsync(l => l.GroupId == createLessonDto.GroupId &&
                              l.WeekIndex == createLessonDto.WeekIndex &&
                              l.DayOfWeekIndex == createLessonDto.DayOfWeekIndex &&
                              l.StartTime == createLessonDto.StartTime &&
                              !l.IsDeleted);
                              
            if (existingLesson)
                return new Response<string>(HttpStatusCode.BadRequest, "Lesson with this schedule already exists");

            // Ҳисоб кардани DayIndex аз рӯи индексҳои ҳафта ва рӯз
            int dayIndex = (createLessonDto.WeekIndex - 1) * 5 + createLessonDto.DayOfWeekIndex;
            
            var lesson = new Lesson
            {
                GroupId = createLessonDto.GroupId,
                StartTime = createLessonDto.StartTime,
                WeekIndex = createLessonDto.WeekIndex,
                DayOfWeekIndex = createLessonDto.DayOfWeekIndex,
                DayIndex = createLessonDto.DayIndex > 0 ? createLessonDto.DayIndex : dayIndex, // Истифодаи қимати додашуда ё ҳисобкардашуда
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await context.Lessons.AddAsync(lesson);
            var result = await context.SaveChangesAsync();

            return result > 0 
                ? new Response<string>(HttpStatusCode.Created, "Lesson created successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to create lesson");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region UpdateLesson
    public async Task<Response<string>> UpdateLesson(UpdateLessonDto updateLessonDto)
    {
        try
        {
            var lesson = await context.Lessons
                .FirstOrDefaultAsync(l => l.Id == updateLessonDto.LessonId && !l.IsDeleted);
                
            if (lesson == null)
                return new Response<string>(HttpStatusCode.NotFound, "Lesson not found");

            var existingLesson = await context.Lessons
                .AnyAsync(l => l.GroupId == lesson.GroupId &&
                              l.WeekIndex == updateLessonDto.WeekIndex &&
                              l.DayOfWeekIndex == updateLessonDto.DayOfWeekIndex &&
                              l.StartTime == updateLessonDto.StartTime &&
                              l.Id != updateLessonDto.LessonId &&
                              !l.IsDeleted);
                              
            if (existingLesson)
                return new Response<string>(HttpStatusCode.BadRequest, "Another lesson with this schedule already exists");

            lesson.StartTime = updateLessonDto.StartTime;
            lesson.WeekIndex = updateLessonDto.WeekIndex;
            lesson.DayOfWeekIndex = updateLessonDto.DayOfWeekIndex;
            
            // Ҳисоб кардани DayIndex нав ё истифодаи қимати додашуда
            if (updateLessonDto.DayIndex > 0)
            {
                lesson.DayIndex = updateLessonDto.DayIndex;
            }
            else
            {
                // Ҳисоб кардани DayIndex аз рӯи индексҳои ҳафта ва рӯз
                lesson.DayIndex = (updateLessonDto.WeekIndex - 1) * 5 + updateLessonDto.DayOfWeekIndex;
            }
            
            lesson.UpdatedAt = DateTimeOffset.UtcNow;

            context.Lessons.Update(lesson);
            var result = await context.SaveChangesAsync();

            return result > 0 
                ? new Response<string>(HttpStatusCode.OK, "Lesson updated successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to update lesson");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region DeleteLesson
    public async Task<Response<string>> DeleteLesson(int id)
    {
        try
        {
            var lesson = await context.Lessons
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
                
            if (lesson == null)
                return new Response<string>(HttpStatusCode.NotFound, "Lesson not found");

            lesson.IsDeleted = true;
            lesson.UpdatedAt = DateTimeOffset.UtcNow;

            context.Lessons.Update(lesson);
            var result = await context.SaveChangesAsync();

            return result > 0 
                ? new Response<string>(HttpStatusCode.OK, "Lesson deleted successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to delete lesson");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetLessonsPaginated
    public async Task<PaginationResponse<List<GetLessonDto>>> GetLessonsPaginated(BaseFilter filter)
    {
        try
        {
            var query = context.Lessons
                .Include(l => l.Group)
                .Where(l => !l.IsDeleted)
                .AsQueryable();
            var totalCount = await query.CountAsync();
            
            var lessons = await query
                .OrderByDescending(l => l.StartTime)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(l => new GetLessonDto
                {
                    Id = l.Id,
                    GroupId = l.GroupId,
                    StartTime = l.StartTime,
                    WeekIndex = l.WeekIndex,
                    DayOfWeekIndex = l.DayOfWeekIndex,
                    GroupName = l.Group.Name ?? "Unknown"
                })
                .ToListAsync();

            return new PaginationResponse<List<GetLessonDto>>(
                lessons,
                filter.PageNumber,
                filter.PageSize,
                totalCount
            );
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetLessonDto>>(
                HttpStatusCode.InternalServerError,
                ex.Message
            );
        }
    }
    #endregion

    #region GetLessonsByGroup
    public async Task<Response<List<GetLessonDto>>> GetLessonsByGroup(int groupId)
    {
        try
        {
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
                
            if (group == null)
                return new Response<List<GetLessonDto>>(HttpStatusCode.NotFound, "Group not found");
            
            var lessons = await context.Lessons
                .Where(l => l.GroupId == groupId && !l.IsDeleted)
                .OrderBy(l => l.WeekIndex)
                .ThenBy(l => l.DayOfWeekIndex)
                .ThenBy(l => l.StartTime)
                .Select(l => new GetLessonDto
                {
                    Id = l.Id,
                    GroupId = l.GroupId,
                    StartTime = l.StartTime,
                    WeekIndex = l.WeekIndex,
                    DayOfWeekIndex = l.DayOfWeekIndex,
                    DayIndex = l.DayIndex,
                    GroupName = group.Name ?? "Unknown"
                })
                .ToListAsync();

            if (lessons.Count == 0)
                return new Response<List<GetLessonDto>>(HttpStatusCode.NotFound, "No lessons found for this group");

            return new Response<List<GetLessonDto>>(lessons);
        }
        catch (Exception ex)
        {
            return new Response<List<GetLessonDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region CreateWeeklyLessons
    public async Task<Response<string>> CreateWeeklyLessons(int groupId, int weekIndex, DateTimeOffset startDate)
    {
        try
        {
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
                
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Group not found");

            var existingLessons = await context.Lessons
                .Where(l => l.GroupId == groupId && l.WeekIndex == weekIndex && !l.IsDeleted)
                .ToListAsync();

            if (!existingLessons.Any())
                return new Response<string>(HttpStatusCode.NotFound, "No lesson templates found for this week index");
            
            var newLessons = new List<Lesson>();
            var startOfWeek = startDate.Date;

            foreach (var template in existingLessons)
            {
                var lessonDay = startOfWeek.AddDays(template.DayOfWeekIndex);
                
                // Ҳисоби индекси умумии рӯз барои дарс
                int dayIndex = (template.WeekIndex - 1) * 5 + template.DayOfWeekIndex;
                
                var newLesson = new Lesson
                {
                    GroupId = template.GroupId,
                    StartTime = new DateTimeOffset(
                        lessonDay.Year, lessonDay.Month, lessonDay.Day,
                        template.StartTime.Hour, template.StartTime.Minute, 0,
                        template.StartTime.Offset),
                    WeekIndex = template.WeekIndex,
                    DayOfWeekIndex = template.DayOfWeekIndex,
                    DayIndex = template.DayIndex > 0 ? template.DayIndex : dayIndex, // Истифодаи қимати мавҷуда ё ҳисобшуда
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                
                newLessons.Add(newLesson);
            }

            await context.Lessons.AddRangeAsync(newLessons);
            var result = await context.SaveChangesAsync();

            return result > 0 
                ? new Response<string>(HttpStatusCode.Created, $"Successfully created {newLessons.Count} lessons for the week")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to create weekly lessons");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region MarkStudentPresent
    public async Task<Response<string>> MarkStudentPresent(int lessonId, int studentId)
    {
        try
        {
            var lesson = await context.Lessons
                .Include(l => l.Group)
                .FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted);
                
            if (lesson == null)
                return new Response<string>(HttpStatusCode.NotFound, "Урок не найден");
            var student = await context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
                
            if (student == null)
                return new Response<string>(HttpStatusCode.NotFound, "Студент не найден");
            var studentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == studentId && 
                                      sg.GroupId == lesson.GroupId && 
                                      sg.IsActive == true && 
                                      !sg.IsDeleted);
                
            if (studentGroup == null)
                return new Response<string>(HttpStatusCode.BadRequest, "Студент не принадлежит к группе этого урока");

            var existingGrade = await context.Grades
                .FirstOrDefaultAsync(g => g.StudentId == studentId && 
                                    g.LessonId == lessonId && 
                                    !g.IsDeleted);
            
            if (existingGrade != null)
            {
                existingGrade.BonusPoints = (existingGrade.BonusPoints ?? 0) + 1;
                existingGrade.UpdatedAt = DateTimeOffset.UtcNow;
                context.Grades.Update(existingGrade);
            }
            else
            {
                var grade = new Grade
                {
                    StudentId = studentId,
                    GroupId = lesson.GroupId,
                    LessonId = lessonId,
                    Value = null, // Основная оценка может быть не выставлена
                    BonusPoints = 1, // Ставим 1 бонусный балл
                    Comment = "Бонусный балл за присутствие на уроке",
                    WeekIndex = lesson.WeekIndex,
                    DayIndex = lesson.DayIndex, // Добавляем индекс дня из урока
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                
                await context.Grades.AddAsync(grade);
            }

            var attendance = new Attendance
            {
                Status = Domain.Enums.AttendanceStatus.Present,
                StudentId = studentId,
                LessonId = lessonId,
                GroupId = lesson.GroupId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            
            await context.Attendances.AddAsync(attendance);
            
            var result = await context.SaveChangesAsync();

            return result > 0 
                ? new Response<string>(HttpStatusCode.OK, $"Студент {student.User?.FullName} успешно отмечен на уроке и получил бонусный балл")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось отметить студента на уроке");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion
}