using System.Net;
using Domain.DTOs.Group;
using Domain.DTOs.Attendance;
using Domain.Entities;
using Domain.Enums;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class GroupService(DataContext context, string uploadPath) : IGroupService
{
    private readonly string[] _allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
    private const long MaxImageSize = 50 * 1024 * 1024; // 50MB

    #region CreateGroupAsync
    public async Task<Response<string>> CreateGroupAsync(CreateGroupDto request)
    {
        try
        {
            // Проверка существования курса
            var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId);
            if (course == null)
                return new Response<string>(HttpStatusCode.NotFound, "Course not found");

            // Проверка существования преподавателя
            var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == request.MentorId);
            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Mentor not found");

            // Проверка уникальности имени группы
            var existingGroup = await context.Groups.AnyAsync(g => g.Name == request.Name);
            if (existingGroup)
                return new Response<string>(HttpStatusCode.BadRequest, "Group with this name already exists");

            // Обработка изображения группы, если оно было загружено
            string imagePath = string.Empty;
            if (request.Image != null)
            {
                // Проверка расширения файла
                var fileExtension = Path.GetExtension(request.Image.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(fileExtension))
                    return new Response<string>(HttpStatusCode.BadRequest,
                        "Invalid image format. Allowed formats: .jpg, .jpeg, .png, .gif");

                // Проверка размера файла
                if (request.Image.Length > MaxImageSize)
                    return new Response<string>(HttpStatusCode.BadRequest,
                        "Image size must be less than 50MB");

                // Создание директории, если не существует
                var groupsFolder = Path.Combine(uploadPath, "uploads", "groups");
                if (!Directory.Exists(groupsFolder))
                    Directory.CreateDirectory(groupsFolder);

                // Создание уникального имени файла и сохранение изображения
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(groupsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.Image.CopyToAsync(fileStream);
                }

                imagePath = $"/uploads/groups/{uniqueFileName}";
            }

            // Вычисление общего количества недель на основе длительности курса
            var totalWeeks = request.DurationMonth * 4; // Примерно 4 недели в месяце

            // Создание объекта группы
            var group = new Group
            {
                Name = request.Name,
                Description = request.Description,
                CourseId = request.CourseId,
                DurationMonth = request.DurationMonth,
                LessonInWeek = request.LessonInWeek,
                HasWeeklyExam = request.HasWeeklyExam,
                TotalWeeks = totalWeeks,
                Started = request.Started,
                Status = request.Status,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                MentorId = request.MentorId,
                PhotoPath = imagePath,
                CurrentWeek = request.CurrentWeek
            };

            // Добавление группы в базу данных
            await context.Groups.AddAsync(group);
            var result = await context.SaveChangesAsync();

            if (result > 0)
                return new Response<string>(HttpStatusCode.Created, "Group created successfully");
            
            return new Response<string>(HttpStatusCode.InternalServerError, "Failed to create group");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region UpdateGroupAsync
    public async Task<Response<string>> UpdateGroupAsync(int id, UpdateGroupDto request)
    {
        try
        {
            // Проверка существования группы
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == id);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Group not found");

            // Проверка существования курса
            var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId);
            if (course == null)
                return new Response<string>(HttpStatusCode.NotFound, "Course not found");

            // Проверка существования преподавателя
            var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == request.MentorId);
            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Mentor not found");

            // Проверка уникальности имени группы (если имя изменилось)
            if (group.Name != request.Name)
            {
                var existingGroup = await context.Groups.AnyAsync(g => g.Name == request.Name && g.Id != id);
                if (existingGroup)
                    return new Response<string>(HttpStatusCode.BadRequest, "Group with this name already exists");
            }

            // Обработка изображения группы, если оно было загружено
            if (request.Image != null)
            {
                // Проверка расширения файла
                var fileExtension = Path.GetExtension(request.Image.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(fileExtension))
                    return new Response<string>(HttpStatusCode.BadRequest,
                        "Invalid image format. Allowed formats: .jpg, .jpeg, .png, .gif");

                // Проверка размера файла
                if (request.Image.Length > MaxImageSize)
                    return new Response<string>(HttpStatusCode.BadRequest,
                        "Image size must be less than 50MB");

                // Создание директории, если не существует
                var groupsFolder = Path.Combine(uploadPath, "uploads", "groups");
                if (!Directory.Exists(groupsFolder))
                    Directory.CreateDirectory(groupsFolder);

                // Создание уникального имени файла и сохранение изображения
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(groupsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.Image.CopyToAsync(fileStream);
                }

                // Удаление старого изображения, если оно существовало
                if (!string.IsNullOrEmpty(group.PhotoPath))
                {
                    var oldImagePath = Path.Combine(uploadPath, group.PhotoPath.TrimStart('/'));
                    if (File.Exists(oldImagePath))
                    {
                        File.Delete(oldImagePath);
                    }
                }

                group.PhotoPath = $"/uploads/groups/{uniqueFileName}";
            }

            // Вычисление общего количества недель на основе длительности курса
            var totalWeeks = request.DurationMonth * 4; // Примерно 4 недели в месяце

            // Обновление свойств группы
            group.Name = request.Name;
            group.Description = request.Description;
            group.CourseId = request.CourseId;
            group.DurationMonth = request.DurationMonth;
            group.LessonInWeek = request.LessonInWeek;
            group.HasWeeklyExam = request.HasWeeklyExam;
            group.TotalWeeks = totalWeeks;
            group.Started = request.Started;
            group.Status = request.Status;
            group.StartDate = request.StartDate;
            group.EndDate = request.EndDate;
            group.MentorId = request.MentorId;
            group.CurrentWeek = request.CurrentWeek;

            // Обновление группы в базе данных
            context.Groups.Update(group);
            var result = await context.SaveChangesAsync();

            if (result > 0)
                return new Response<string>(HttpStatusCode.OK, "Group updated successfully");
            
            return new Response<string>(HttpStatusCode.InternalServerError, "Failed to update group");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region DeleteGroupAsync
    public async Task<Response<string>> DeleteGroupAsync(int id)
    {
        try
        {
            // Проверка существования группы
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == id);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Group not found");

            // Проверка активных студентов в группе
            var activeStudentsInGroup = await context.StudentGroups
                .Where(sg => sg.GroupId == id && sg.IsActive == true)
                .CountAsync();

            if (activeStudentsInGroup > 0)
                return new Response<string>(HttpStatusCode.BadRequest, 
                    $"Cannot delete group because it has {activeStudentsInGroup} active students");

            // Проверка активных занятий в группе
            var activeLessons = await context.Lessons
                .Where(l => l.GroupId == id)
                .CountAsync();

            if (activeLessons > 0)
                return new Response<string>(HttpStatusCode.BadRequest, 
                    $"Cannot delete group because it has {activeLessons} active lessons");

            // Мягкое удаление - устанавливаем флаг IsDeleted
            group.IsDeleted = true;
            
            // Сохранение изменений
            var result = await context.SaveChangesAsync();

            if (result > 0)
                return new Response<string>(HttpStatusCode.OK, "Group deleted successfully");
            
            return new Response<string>(HttpStatusCode.InternalServerError, "Failed to delete group");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetGroupByIdAsync
    public async Task<Response<GetGroupDto>> GetGroupByIdAsync(int id)
    {
        try
        {
            // Получение группы с подсчетом студентов
            var group = await context.Groups
                .Include(g => g.StudentGroups)
                .Where(g => g.Id == id && !g.IsDeleted)
                .Select(g => new GetGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    CourseId = g.CourseId,
                    DurationMonth = g.DurationMonth,
                    LessonInWeek = g.LessonInWeek,
                    TotalWeeks = g.TotalWeeks,
                    Started = g.Started,
                    Status = g.Status,
                    StartDate = g.StartDate,
                    EndDate = g.EndDate,
                    MentorId = g.MentorId,
                    ImagePath = g.PhotoPath,
                    CurrentWeek = g.CurrentWeek,
                    CurrentStudentsCount = g.StudentGroups.Count(sg => sg.IsActive == true)
                })
                .FirstOrDefaultAsync();

            if (group == null)
                return new Response<GetGroupDto>(HttpStatusCode.NotFound, "Group not found");

            return new Response<GetGroupDto>(group);
        }
        catch (Exception ex)
        {
            return new Response<GetGroupDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetGroups
    public async Task<Response<List<GetGroupDto>>> GetGroups()
    {
        try
        {
            // Получение всех групп с подсчетом студентов
            var groups = await context.Groups
                .Include(g => g.StudentGroups)
                .Where(g => !g.IsDeleted)
                .Select(g => new GetGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    CourseId = g.CourseId,
                    DurationMonth = g.DurationMonth,
                    LessonInWeek = g.LessonInWeek,
                    TotalWeeks = g.TotalWeeks,
                    Started = g.Started,
                    Status = g.Status,
                    StartDate = g.StartDate,
                    EndDate = g.EndDate,
                    MentorId = g.MentorId,
                    ImagePath = g.PhotoPath,
                    CurrentWeek = g.CurrentWeek,
                    CurrentStudentsCount = g.StudentGroups.Count(sg => sg.IsActive == true)
                })
                .ToListAsync();

            return new Response<List<GetGroupDto>>(groups);
        }
        catch (Exception ex)
        {
            return new Response<List<GetGroupDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetGroupPaginated
    public async Task<PaginationResponse<List<GetGroupDto>>> GetGroupPaginated(GroupFilter filter)
    {
        try
        {
            // Базовый запрос к группам
            var query = context.Groups
                .Include(g => g.StudentGroups)
                .Where(g => !g.IsDeleted)
                .AsQueryable();

            // Применение фильтров
            // Фильтр по имени
            if (!string.IsNullOrEmpty(filter.Name))
                query = query.Where(g => g.Name.Contains(filter.Name));

            // Фильтр по курсу
            if (filter.CourseId.HasValue)
                query = query.Where(g => g.CourseId == filter.CourseId.Value);

            // Фильтр по преподавателю
            if (filter.MentorId.HasValue)
                query = query.Where(g => g.MentorId == filter.MentorId.Value);

            // Фильтр по статусу Started
            if (filter.Started.HasValue)
                query = query.Where(g => g.Started == filter.Started.Value);

            // Фильтр по статусу активности
            if (filter.Status.HasValue)
                query = query.Where(g => g.Status == filter.Status.Value);

            // Фильтр по дате начала (от)
            if (filter.StartDateFrom.HasValue)
                query = query.Where(g => g.StartDate >= new DateTimeOffset(filter.StartDateFrom.Value));

            // Фильтр по дате начала (до)
            if (filter.StartDateTo.HasValue)
                query = query.Where(g => g.StartDate <= new DateTimeOffset(filter.StartDateTo.Value));

            // Фильтр по дате окончания (от)
            if (filter.EndDateFrom.HasValue)
                query = query.Where(g => g.EndDate >= new DateTimeOffset(filter.EndDateFrom.Value));

            // Фильтр по дате окончания (до)
            if (filter.EndDateTo.HasValue)
                query = query.Where(g => g.EndDate <= new DateTimeOffset(filter.EndDateTo.Value));

            // Получение общего количества записей для пагинации
            var totalRecords = await query.CountAsync();

            // Сортировка по умолчанию по Id
            query = query.OrderBy(g => g.Id);

            // Применение пагинации
            query = query.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize);

            // Формирование результата с подсчетом студентов
            var groups = await query
                .Select(g => new GetGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    CourseId = g.CourseId,
                    DurationMonth = g.DurationMonth,
                    LessonInWeek = g.LessonInWeek,
                    TotalWeeks = g.TotalWeeks,
                    Started = g.Started,
                    Status = g.Status,
                    StartDate = g.StartDate,
                    EndDate = g.EndDate,
                    MentorId = g.MentorId,
                    ImagePath = g.PhotoPath,
                    CurrentWeek = g.CurrentWeek,
                    CurrentStudentsCount = g.StudentGroups.Count(sg => sg.IsActive == true)
                })
                .ToListAsync();

            // Создание объекта пагинации
            return new PaginationResponse<List<GetGroupDto>>(
                groups,
                filter.PageNumber,
                filter.PageSize,
                totalRecords
            );
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetGroupDto>>(
                HttpStatusCode.InternalServerError,
                ex.Message
            );
        }
    }
    #endregion

    #region GetGroupAttendanceStatisticsAsync
    public async Task<Response<GroupAttendanceStatisticsDto>> GetGroupAttendanceStatisticsAsync(int groupId)
    {
        try
        {
            // Проверяем существование группы
            var group = await context.Groups
                .Include(g => g.StudentGroups)
                .ThenInclude(sg => sg.Student)
                .Include(g => g.Lessons)
                .ThenInclude(l => l.Attendances)
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

            if (group == null)
                return new Response<GroupAttendanceStatisticsDto>(HttpStatusCode.NotFound, "Group not found");

            // Получаем количество активных студентов в группе
            var activeStudents = group.StudentGroups.Count(sg => sg.IsActive == true);

            // Создаем статистику
            var statistics = new GroupAttendanceStatisticsDto
            {
                GroupId = group.Id,
                GroupName = group.Name,
                TotalStudents = activeStudents,
                CurrentWeek = group.CurrentWeek
            };

            // Получаем все посещения по группе
            var allAttendances = group.Lessons
                .SelectMany(l => l.Attendances)
                .ToList();

            // Подсчитываем общую статистику
            statistics.TotalPresentCount = allAttendances.Count(a => a.Status == AttendanceStatus.Present);
            statistics.TotalAbsentCount = allAttendances.Count(a => a.Status == AttendanceStatus.Absent);
            statistics.TotalLateCount = allAttendances.Count(a => a.Status == AttendanceStatus.Late);

            var totalAttendances = statistics.TotalPresentCount + statistics.TotalAbsentCount + statistics.TotalLateCount;
            statistics.OverallAttendancePercentage = totalAttendances > 0 
                ? Math.Round((double)(statistics.TotalPresentCount + statistics.TotalLateCount) / totalAttendances * 100, 2) 
                : 0;

            // Группируем посещения по неделям
            var attendancesByWeek = allAttendances
                .GroupBy(a => group.Lessons.FirstOrDefault(l => l.Id == a.LessonId)?.WeekIndex ?? 0)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Заполняем статистику по неделям
            foreach (var weekAttendance in attendancesByWeek)
            {
                int weekNumber = weekAttendance.Key;
                if (weekNumber == 0) continue; // Пропускаем занятия без номера недели

                var presentCount = weekAttendance.Value.Count(a => a.Status == AttendanceStatus.Present);
                var absentCount = weekAttendance.Value.Count(a => a.Status == AttendanceStatus.Absent);
                var lateCount = weekAttendance.Value.Count(a => a.Status == AttendanceStatus.Late);
                var totalWeekAttendances = presentCount + absentCount + lateCount;

                statistics.WeeklyAttendance[weekNumber] = new GroupAttendanceStatisticsDto.WeekAttendanceStatistics
                {
                    WeekNumber = weekNumber,
                    PresentCount = presentCount,
                    AbsentCount = absentCount,
                    LateCount = lateCount,
                    AttendancePercentage = totalWeekAttendances > 0 
                        ? Math.Round((double)(presentCount + lateCount) / totalWeekAttendances * 100, 2) 
                        : 0
                };
            }

            // Получаем последние 10 записей о посещаемости
            statistics.RecentAttendances = group.Lessons
                .OrderByDescending(l => l.StartTime)
                .Take(5)
                .SelectMany(l => l.Attendances)
                .Select(a => new GetAttendanceDto
                {
                    Id = a.Id,
                    Status = a.Status,
                    LessonId = a.LessonId,
                    StudentId = a.StudentId,
                    StudentName = group.StudentGroups.FirstOrDefault(sg => sg.StudentId == a.StudentId)?.Student?.FullName ?? string.Empty,
                    LessonStartTime = group.Lessons.FirstOrDefault(l => l.Id == a.LessonId)?.StartTime ?? DateTimeOffset.MinValue
                })
                .Take(10)
                .ToList();

            return new Response<GroupAttendanceStatisticsDto>(statistics);
        }
        catch (Exception ex)
        {
            return new Response<GroupAttendanceStatisticsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion


}