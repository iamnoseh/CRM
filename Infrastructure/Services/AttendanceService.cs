using System.Net;
using Domain.DTOs.Attendance;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class AttendanceService(DataContext context) : IAttendanceService
{

    private string GetStatusName(AttendanceStatus status)
    {
        return status switch
        {
            AttendanceStatus.Present => "Присутствует",
            AttendanceStatus.Absent => "Отсутствует",
            AttendanceStatus.Late => "Опоздал",
            _ => "Неизвестно"
        };
    }

    public async Task<Response<List<GetAttendanceDto>>> GetAttendances()
    {
        try
        {
            var attendances = await context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Group)
                .Include(a => a.Lesson)
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            if (attendances.Count == 0)
                return new Response<List<GetAttendanceDto>>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Записей о посещаемости не найдено"
                };

            var attendanceDtos = attendances.Select(attendance => new GetAttendanceDto
            {
                Id = attendance.Id,
                Status = attendance.Status,
                LessonId = attendance.LessonId,
                StudentId = attendance.StudentId,
                GroupId = attendance.GroupId,
                StudentName = attendance.Student?.User?.FullName ?? "Неизвестно",
                GroupName = attendance.Group?.Name ?? "Неизвестно",
                LessonStartTime = attendance.Lesson?.StartTime ?? DateTime.UtcNow,
                WeekIndex = attendance.Lesson?.WeekIndex ?? 0,
                DayOfWeekIndex = (int)(attendance.Lesson?.StartTime.DayOfWeek ?? 0),
                StatusName = GetStatusName(attendance.Status),
                CreatedAt = attendance.CreatedAt,
                UpdatedAt = attendance.UpdatedAt,
                IsDeleted = attendance.IsDeleted
            }).ToList();

            return new Response<List<GetAttendanceDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Записи о посещаемости успешно получены",
                Data = attendanceDtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetAttendanceDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Ошибка при получении записей о посещаемости: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetAttendanceDto>> GetAttendanceById(int id)
    {
        try
        {
            var attendance = await context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Group)
                .Include(a => a.Lesson)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (attendance == null)
                return new Response<GetAttendanceDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Запись о посещаемости не найдена"
                };

            var attendanceDto = new GetAttendanceDto
            {
                Id = attendance.Id,
                Status = attendance.Status,
                LessonId = attendance.LessonId,
                StudentId = attendance.StudentId,
                GroupId = attendance.GroupId,
                StudentName = attendance.Student?.User?.FullName ?? "Неизвестно",
                GroupName = attendance.Group?.Name ?? "Неизвестно",
                LessonStartTime = attendance.Lesson?.StartTime ?? DateTime.UtcNow,
                WeekIndex = attendance.Lesson?.WeekIndex ?? 0,
                DayOfWeekIndex = (int)(attendance.Lesson?.StartTime.DayOfWeek ?? 0),
                StatusName = GetStatusName(attendance.Status),
                CreatedAt = attendance.CreatedAt,
                UpdatedAt = attendance.UpdatedAt,
                IsDeleted = attendance.IsDeleted
            };

            return new Response<GetAttendanceDto>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Запись о посещаемости успешно получена",
                Data = attendanceDto
            };
        }
        catch (Exception ex)
        {
            return new Response<GetAttendanceDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Ошибка при получении записи о посещаемости: {ex.Message}"
            };
        }
    }

    public async Task<Response<string>> CreateAttendance(AddAttendanceDto addAttendanceDto)
    {
        try
        {
            // Проверяем существование студента
            var student = await context.Students
                .FirstOrDefaultAsync(s => s.Id == addAttendanceDto.StudentId && !s.IsDeleted);
            
            if (student == null)
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Студент не найден"
                };
            
            var lesson = await context.Lessons
                .FirstOrDefaultAsync(l => l.Id == addAttendanceDto.LessonId && !l.IsDeleted);
            
            if (lesson == null)
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Урок не найден"
                };
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == addAttendanceDto.GroupId && !g.IsDeleted);
            
            if (group == null)
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Группа не найдена"
                };
            var studentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == addAttendanceDto.StudentId && 
                                     sg.GroupId == addAttendanceDto.GroupId && 
                                     sg.IsActive == true && 
                                     !sg.IsDeleted);
            
            if (studentGroup == null)
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Студент не принадлежит к указанной группе"
                };
            
            // Проверяем, относится ли урок к указанной группе
            if (lesson.GroupId != addAttendanceDto.GroupId)
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Урок не относится к указанной группе"
                };
            
            // Проверяем, не отмечен ли уже студент на этом уроке
            var existingAttendance = await context.Attendances
                .FirstOrDefaultAsync(a => a.StudentId == addAttendanceDto.StudentId && 
                                     a.LessonId == addAttendanceDto.LessonId && 
                                     !a.IsDeleted);
            
            if (existingAttendance != null)
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Посещаемость для данного студента на этом уроке уже отмечена"
                };
            
            // Создаем запись о посещаемости
            var attendance = new Attendance
            {
                Status = addAttendanceDto.Status,
                StudentId = addAttendanceDto.StudentId,
                LessonId = addAttendanceDto.LessonId,
                GroupId = addAttendanceDto.GroupId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await context.Attendances.AddAsync(attendance);
            
            // Если студент присутствовал на уроке, добавляем ему бонусный балл
            if (addAttendanceDto.Status == AttendanceStatus.Present)
            {
                // Проверяем, существует ли уже оценка для этого студента и урока
                var existingGrade = await context.Grades
                    .FirstOrDefaultAsync(g => g.StudentId == addAttendanceDto.StudentId && 
                                        g.LessonId == addAttendanceDto.LessonId && 
                                        !g.IsDeleted);
                
                if (existingGrade != null)
                {
                    existingGrade.BonusPoints = (existingGrade.BonusPoints ?? 0) + 1;
                    existingGrade.UpdatedAt = DateTime.UtcNow;
                    context.Grades.Update(existingGrade);
                }
                else
                {
                    var grade = new Grade
                    {
                        StudentId = addAttendanceDto.StudentId,
                        GroupId = addAttendanceDto.GroupId,
                        LessonId = addAttendanceDto.LessonId,
                        Value = null, 
                        BonusPoints = 1, 
                        Comment = "Бонусный балл за присутствие",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    await context.Grades.AddAsync(grade);
                }
            }
            await context.SaveChangesAsync();
            string statusMessage = addAttendanceDto.Status switch
            {
                AttendanceStatus.Present => "Студент отмечен как присутствующий и получил бонусный балл",
                AttendanceStatus.Absent => "Студент отмечен как отсутствующий",
                AttendanceStatus.Late => "Студент отмечен как опоздавший",
                _ => "Студент отмечен на уроке"
            };
            
            return new Response<string>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = statusMessage,
                Data = "Посещаемость успешно добавлена"
            };
        }
        catch (Exception ex)
        {
            return new Response<string>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Ошибка при добавлении посещаемости: {ex.Message}"
            };
        }
    }

    public async Task<Response<string>> DeleteAttendanceById(int id)
    {
        try
        {
            // Проверка существования записи посещаемости
            var attendance = await context.Attendances
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (attendance == null)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = $"Запись о посещаемости с ID {id} не найдена"
                };
            }

            // Выполняем мягкое удаление
            attendance.IsDeleted = true;
            attendance.UpdatedAt = DateTimeOffset.UtcNow;

            // Сохранение изменений
            await context.SaveChangesAsync();

            return new Response<string>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Запись о посещаемости успешно удалена",
                Data = attendance.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            return new Response<string>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Ошибка при удалении записи о посещаемости: {ex.Message}"
            };
        }
    }

    public async Task<Response<string>> EditAttendance(EditAttendanceDto editAttendanceDto)
    {
        try
        {
            // Проверка входных данных
            if (editAttendanceDto == null)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Данные для обновления посещаемости не предоставлены"
                };
            }

            // Проверка существования посещаемости
            var attendance = await context.Attendances
                .FirstOrDefaultAsync(a => a.Id == editAttendanceDto.Id && !a.IsDeleted);

            if (attendance == null)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = $"Посещаемость с ID {editAttendanceDto.Id} не найдена"
                };
            }

            // Проверка существования студента
            var student = await context.Students
                .FirstOrDefaultAsync(s => s.Id == editAttendanceDto.StudentId && !s.IsDeleted);

            if (student == null)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = $"Студент с ID {editAttendanceDto.StudentId} не найден"
                };
            }

            // Проверка существования урока
            var lesson = await context.Lessons
                .FirstOrDefaultAsync(l => l.Id == editAttendanceDto.LessonId && !l.IsDeleted);

            if (lesson == null)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = $"Урок с ID {editAttendanceDto.LessonId} не найден"
                };
            }

            // Проверка существования группы
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == editAttendanceDto.GroupId && !g.IsDeleted);

            if (group == null)
            {
                return new Response<string>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = $"Группа с ID {editAttendanceDto.GroupId} не найдена"
                };
            }

            // Обновление полей
            attendance.Status = editAttendanceDto.Status;
            attendance.LessonId = editAttendanceDto.LessonId;
            attendance.StudentId = editAttendanceDto.StudentId;
            attendance.GroupId = editAttendanceDto.GroupId;
            attendance.UpdatedAt = DateTimeOffset.UtcNow;

            // Сохранение изменений
            await context.SaveChangesAsync();

            return new Response<string>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Данные о посещаемости успешно обновлены",
                Data = attendance.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            return new Response<string>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Ошибка при обновлении данных о посещаемости: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetAttendanceDto>>> GetAttendancesByStudent(int studentId)
    {
        try
        {
            var student = await context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            
            if (student == null)
                return new Response<List<GetAttendanceDto>>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Студент не найден"
                };
            
            var attendances = await context.Attendances
                .Include(a => a.Lesson)
                .Include(a => a.Group)
                .Where(a => a.StudentId == studentId && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            
            if (attendances.Count == 0)
                return new Response<List<GetAttendanceDto>>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Записи о посещаемости для данного студента не найдены"
                };
            
            var attendanceDtos = attendances.Select(a => new GetAttendanceDto
            {
                Id = a.Id,
                Status = a.Status,
                LessonId = a.LessonId,
                StudentId = a.StudentId,
                GroupId = a.GroupId,
                StudentName = student.User?.FullName ?? "Неизвестно",
                GroupName = a.Group?.Name ?? "Неизвестно",
                LessonStartTime = a.Lesson?.StartTime ?? DateTime.UtcNow,
                WeekIndex = a.Lesson?.WeekIndex ?? 0,
                DayOfWeekIndex = (int)(a.Lesson?.StartTime.DayOfWeek ?? 0),
                StatusName = GetStatusName(a.Status),
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                IsDeleted = a.IsDeleted
            }).ToList();
            
            return new Response<List<GetAttendanceDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Записи о посещаемости для студента успешно получены",
                Data = attendanceDtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetAttendanceDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Ошибка при получении записей о посещаемости: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetAttendanceDto>>> GetAttendancesByGroup(int groupId)
    {
        try
        {
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            
            if (group == null)
                return new Response<List<GetAttendanceDto>>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Группа не найдена"
                };
            
            var attendances = await context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Lesson)
                .Where(a => a.GroupId == groupId && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            
            if (attendances.Count == 0)
                return new Response<List<GetAttendanceDto>>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Записи о посещаемости для данной группы не найдены"
                };
            
            var attendanceDtos = attendances.Select(a => new GetAttendanceDto
            {
                Id = a.Id,
                Status = a.Status,
                LessonId = a.LessonId,
                StudentId = a.StudentId,
                GroupId = a.GroupId,
                StudentName = a.Student?.User?.FullName ?? "Неизвестно",
                GroupName = group.Name,
                LessonStartTime = a.Lesson?.StartTime ?? DateTime.UtcNow,
                WeekIndex = a.Lesson?.WeekIndex ?? 0,
                DayOfWeekIndex = (int)(a.Lesson?.StartTime.DayOfWeek ?? 0),
                StatusName = GetStatusName(a.Status),
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                IsDeleted = a.IsDeleted
            }).ToList();
            
            return new Response<List<GetAttendanceDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Записи о посещаемости для группы успешно получены",
                Data = attendanceDtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetAttendanceDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Ошибка при получении записей о посещаемости: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetAttendanceDto>>> GetAttendancesByLesson(int lessonId)
    {
        try
        {
            var lesson = await context.Lessons
                .Include(l => l.Group)
                .FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted);
            
            if (lesson == null)
                return new Response<List<GetAttendanceDto>>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Урок не найден"
                };
            
            var attendances = await context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Where(a => a.LessonId == lessonId && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            
            if (attendances.Count == 0)
                return new Response<List<GetAttendanceDto>>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Записи о посещаемости для данного урока не найдены"
                };
            
            var attendanceDtos = attendances.Select(a => new GetAttendanceDto
            {
                Id = a.Id,
                Status = a.Status,
                LessonId = a.LessonId,
                StudentId = a.StudentId,
                GroupId = a.GroupId,
                StudentName = a.Student?.User?.FullName ?? "Неизвестно",
                GroupName = lesson.Group?.Name ?? "Неизвестно",
                LessonStartTime = lesson.StartTime,
                WeekIndex = lesson.WeekIndex,
                DayOfWeekIndex = (int)lesson.StartTime.DayOfWeek,
                StatusName = GetStatusName(a.Status),
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                IsDeleted = a.IsDeleted
            }).ToList();
            
            return new Response<List<GetAttendanceDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Записи о посещаемости для урока успешно получены",
                Data = attendanceDtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetAttendanceDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Ошибка при получении записей о посещаемости: {ex.Message}"
            };
        }
    }

    public async Task<Response<double>> GetStudentAttendanceRate(int studentId, int? groupId = null)
    {
        try
        {
            var student = await context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            
            if (student == null)
                return new Response<double>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Студент не найден"
                };
            
            IQueryable<Attendance> attendancesQuery = context.Attendances
                .Where(a => a.StudentId == studentId && !a.IsDeleted);
            
            if (groupId.HasValue)
            {
                var group = await context.Groups
                    .FirstOrDefaultAsync(g => g.Id == groupId.Value && !g.IsDeleted);
                
                if (group == null)
                    return new Response<double>
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Message = "Группа не найдена"
                    };
                
                var studentInGroup = await context.StudentGroups
                    .AnyAsync(sg => sg.StudentId == studentId && 
                                sg.GroupId == groupId.Value && 
                                sg.IsActive == true && 
                                !sg.IsDeleted);
                
                if (!studentInGroup)
                    return new Response<double>
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = "Студент не принадлежит к указанной группе"
                    };
                
                attendancesQuery = attendancesQuery.Where(a => a.GroupId == groupId.Value);
            }
            
            var attendances = await attendancesQuery.ToListAsync();
            
            if (attendances.Count == 0)
                return new Response<double>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Записи о посещаемости для данного студента не найдены"
                };
            
            int totalAttendances = attendances.Count;
            int presentAttendances = attendances.Count(a => a.Status == AttendanceStatus.Present);
            int lateAttendances = attendances.Count(a => a.Status == AttendanceStatus.Late);
            
            double attendanceRate = (presentAttendances + (0.5 * lateAttendances)) / totalAttendances;
            attendanceRate = Math.Round(attendanceRate * 100, 2);
            
            return new Response<double>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = $"Коэффициент посещаемости студента: {attendanceRate}%",
                Data = attendanceRate
            };
        }
        catch (Exception ex)
        {
            return new Response<double>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Ошибка при расчете коэффициента посещаемости: {ex.Message}"
            };
        }
    }
}