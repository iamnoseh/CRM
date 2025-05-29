using System.Net;
using Domain.DTOs.Group;
using Domain.DTOs.Attendance;
using Domain.DTOs.Statistics;
using Domain.Entities;
using Domain.Enums;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Extensions;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GroupAttendanceStatisticsDto = Domain.DTOs.Group.GroupAttendanceStatisticsDto;

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
            var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId);
            if (course == null)
                return new Response<string>(HttpStatusCode.NotFound, "Course not found");

            var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == request.MentorId);
            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Mentor not found");

            var existingGroup = await context.Groups.AnyAsync(g => g.Name == request.Name);
            if (existingGroup)
                return new Response<string>(HttpStatusCode.BadRequest, "Group with this name already exists");

            string imagePath = string.Empty;
            if (request.Image != null)
            {
                var fileExtension = Path.GetExtension(request.Image.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(fileExtension))
                    return new Response<string>(HttpStatusCode.BadRequest,
                        "Invalid image format. Allowed formats: .jpg, .jpeg, .png, .gif");

                if (request.Image.Length > MaxImageSize)
                    return new Response<string>(HttpStatusCode.BadRequest,
                        "Image size must be less than 50MB");

                var groupsFolder = Path.Combine(uploadPath, "uploads", "groups");
                if (!Directory.Exists(groupsFolder))
                    Directory.CreateDirectory(groupsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(groupsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.Image.CopyToAsync(fileStream);
                }

                imagePath = $"/uploads/groups/{uniqueFileName}";
            }
            var approximateTotalDays = request.DurationMonth * 30.44;
            var totalWeeks = (int)Math.Ceiling(approximateTotalDays / 7);
            
            var group = new Group
            {
                Name = request.Name,
                Description = request.Description,
                CourseId = request.CourseId,
                DurationMonth = request.DurationMonth,
                LessonInWeek = request.LessonInWeek,
                HasWeeklyExam = request.HasWeeklyExam,
                TotalWeeks = totalWeeks,
                Started = false, // Always false until activation
                Status = ActiveStatus.Inactive, // Always inactive until activation
                MentorId = request.MentorId,
                PhotoPath = imagePath,
                CurrentWeek = 1, 
                StartDate = DateTimeOffset.MinValue, 
                EndDate = DateTimeOffset.MinValue    
            };

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
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == id);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Group not found");
            
            var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId);
            if (course == null)
                return new Response<string>(HttpStatusCode.NotFound, "Course not found");
            
            var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == request.MentorId);
            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Mentor not found");

            if (group.Name != request.Name)
            {
                var existingGroup = await context.Groups.AnyAsync(g => g.Name == request.Name && g.Id != id);
                if (existingGroup)
                    return new Response<string>(HttpStatusCode.BadRequest, "Group with this name already exists");
            }

            if (request.Image != null)
            {
                var fileExtension = Path.GetExtension(request.Image.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(fileExtension))
                    return new Response<string>(HttpStatusCode.BadRequest,
                        "Invalid image format. Allowed formats: .jpg, .jpeg, .png, .gif");
                
                if (request.Image.Length > MaxImageSize)
                    return new Response<string>(HttpStatusCode.BadRequest,
                        "Image size must be less than 50MB");
                
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

            // Calculate approximate total weeks based on average days per month
            // Average month length is approximately 30.44 days (365.25 / 12)
            var approximateTotalDays = request.DurationMonth * 30.44;
            var totalWeeks = (int)Math.Ceiling(approximateTotalDays / 7);
            
            group.Name = request.Name;
            group.Description = request.Description;
            group.CourseId = request.CourseId;
            group.DurationMonth = request.DurationMonth;
            group.LessonInWeek = request.LessonInWeek;
            group.HasWeeklyExam = request.HasWeeklyExam;
            group.TotalWeeks = totalWeeks;
            group.MentorId = request.MentorId;
            
            // Don't change status, started, start date, end date, or current week
            // These are managed by the activation service
            // The status and started values remain unchanged during update
            
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
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == id);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Group not found");
            
            var activeStudentsInGroup = await context.StudentGroups
                .Where(sg => sg.GroupId == id && sg.IsActive == true)
                .CountAsync();

            if (activeStudentsInGroup > 0)
                return new Response<string>(HttpStatusCode.BadRequest, 
                    $"Cannot delete group because it has {activeStudentsInGroup} active students");

            var activeLessons = await context.Lessons
                .Where(l => l.GroupId == id)
                .CountAsync();

            if (activeLessons > 0)
                return new Response<string>(HttpStatusCode.BadRequest, 
                    $"Cannot delete group because it has {activeLessons} active lessons");

            group.IsDeleted = true;
            
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

            // Ensure DayOfWeek and CurrentWeek have valid values
            group = group.EnsureValidValues();

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
            var query = context.Groups
                .Include(g => g.StudentGroups)
                .Where(g => !g.IsDeleted)
                .AsQueryable();
            
            if (!string.IsNullOrEmpty(filter.Name))
                query = query.Where(g => g.Name.Contains(filter.Name));

            if (filter.CourseId.HasValue)
                query = query.Where(g => g.CourseId == filter.CourseId.Value);
            
            if (filter.MentorId.HasValue)
                query = query.Where(g => g.MentorId == filter.MentorId.Value);
            
            if (filter.Started.HasValue)
                query = query.Where(g => g.Started == filter.Started.Value);
            
            if (filter.Status.HasValue)
                query = query.Where(g => g.Status == filter.Status.Value);
            
            if (filter.StartDateFrom.HasValue)
                query = query.Where(g => g.StartDate >= new DateTimeOffset(filter.StartDateFrom.Value));
            
            if (filter.StartDateTo.HasValue)
                query = query.Where(g => g.StartDate <= new DateTimeOffset(filter.StartDateTo.Value));
            
            if (filter.EndDateFrom.HasValue)
                query = query.Where(g => g.EndDate >= new DateTimeOffset(filter.EndDateFrom.Value));
            
            if (filter.EndDateTo.HasValue)
                query = query.Where(g => g.EndDate <= new DateTimeOffset(filter.EndDateTo.Value));


            var totalRecords = await query.CountAsync();
            
            query = query.OrderBy(g => g.Id);
            
            query = query.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize);
            
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
                if (weekNumber == 0) continue; 

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