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
using GroupAttendanceStatisticsDto = Domain.DTOs.Group.GroupAttendanceStatisticsDto;
using Infrastructure.Helpers;

namespace Infrastructure.Services;

public class GroupService(DataContext context, string uploadPath, IHttpContextAccessor httpContextAccessor) : IGroupService
{
    private readonly string[] _allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
    private const long MaxImageSize = 50 * 1024 * 1024; 
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
                Started = false,
                Status = ActiveStatus.Inactive, 
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
        var groupsQuery = context.Groups
            .Include(g => g.Course)
            .Include(g => g.StudentGroups)
            .Where(g => g.Id == id && !g.IsDeleted);
        groupsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            groupsQuery, httpContextAccessor, g => g.Course.CenterId);
        var group = await groupsQuery.FirstOrDefaultAsync();
        if (group == null)
            return new Response<GetGroupDto>(System.Net.HttpStatusCode.NotFound, "Group not found");
        var dto = new GetGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            CourseId = group.CourseId,
            DurationMonth = group.DurationMonth,
            LessonInWeek = group.LessonInWeek,
            TotalWeeks = group.TotalWeeks,
            Started = group.Started,
            Status = group.Status,
            StartDate = group.StartDate,
            EndDate = group.EndDate,
            MentorId = group.MentorId,
            ImagePath = group.PhotoPath,
            CurrentWeek = group.CurrentWeek,
            CurrentStudentsCount = group.StudentGroups?.Count(sg => sg.IsActive == true) ?? 0
        };
        return new Response<GetGroupDto>(dto);
    }
    #endregion

    #region GetGroups
    public async Task<Response<List<GetGroupDto>>> GetGroups()
    {
        var groupsQuery = context.Groups
            .Include(g => g.Course)
            .Include(g => g.StudentGroups)
            .Where(g => !g.IsDeleted);
        groupsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            groupsQuery, httpContextAccessor, g => g.Course.CenterId);
        var groups = await groupsQuery.ToListAsync();
        var dtos = groups.Select(group => new GetGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            CourseId = group.CourseId,
            DurationMonth = group.DurationMonth,
            LessonInWeek = group.LessonInWeek,
            TotalWeeks = group.TotalWeeks,
            Started = group.Started,
            Status = group.Status,
            StartDate = group.StartDate,
            EndDate = group.EndDate,
            MentorId = group.MentorId,
            ImagePath = group.PhotoPath,
            CurrentWeek = group.CurrentWeek,
            CurrentStudentsCount = group.StudentGroups?.Count(sg => sg.IsActive == true) ?? 0
        }).ToList();
        return new Response<List<GetGroupDto>>(dtos);
    }
    #endregion

    #region GetGroupPaginated
    public async Task<PaginationResponse<List<GetGroupDto>>> GetGroupPaginated(GroupFilter filter)
    {
        var query = context.Groups
            .Include(g => g.Course)
            .Include(g => g.StudentGroups)
            .Where(g => !g.IsDeleted);
        query = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            query, httpContextAccessor, g => g.Course.CenterId);
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
        var groups = await query.ToListAsync();
        var dtos = groups.Select(group => new GetGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            CourseId = group.CourseId,
            DurationMonth = group.DurationMonth,
            LessonInWeek = group.LessonInWeek,
            TotalWeeks = group.TotalWeeks,
            Started = group.Started,
            Status = group.Status,
            StartDate = group.StartDate,
            EndDate = group.EndDate,
            MentorId = group.MentorId,
            ImagePath = group.PhotoPath,
            CurrentWeek = group.CurrentWeek,
            CurrentStudentsCount = group.StudentGroups?.Count(sg => sg.IsActive == true) ?? 0
        }).ToList();
        return new PaginationResponse<List<GetGroupDto>>(
            dtos,
            filter.PageNumber,
            filter.PageSize,
            totalRecords
        );
    }
    #endregion

    #region GetGroupAttendanceStatisticsAsync
    public async Task<Response<GroupAttendanceStatisticsDto>> GetGroupAttendanceStatisticsAsync(int groupId)
    {
        var groupsQuery = context.Groups
            .Include(g => g.StudentGroups)
            .ThenInclude(sg => sg.Student)
            .Include(g => g.Lessons)
            .ThenInclude(l => l.Attendances)
            .Include(g => g.Course)
            .Where(g => g.Id == groupId && !g.IsDeleted);
        groupsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
            groupsQuery, httpContextAccessor, g => g.Course.CenterId);
        var group = await groupsQuery.FirstOrDefaultAsync();
        if (group == null)
            return new Response<GroupAttendanceStatisticsDto>(HttpStatusCode.NotFound, "Group not found");
        var activeStudents = group.StudentGroups.Count(sg => sg.IsActive == true);
        var statistics = new GroupAttendanceStatisticsDto
        {
            GroupId = group.Id,
            GroupName = group.Name,
            TotalStudents = activeStudents,
            CurrentWeek = group.CurrentWeek
        };
        var allAttendances = group.Lessons
            .SelectMany(l => l.Attendances)
            .ToList();
        statistics.TotalPresentCount = allAttendances.Count(a => a.Status == AttendanceStatus.Present);
        statistics.TotalAbsentCount = allAttendances.Count(a => a.Status == AttendanceStatus.Absent);
        statistics.TotalLateCount = allAttendances.Count(a => a.Status == AttendanceStatus.Late);

        var totalAttendances = statistics.TotalPresentCount + statistics.TotalAbsentCount + statistics.TotalLateCount;
        statistics.OverallAttendancePercentage = totalAttendances > 0 
            ? Math.Round((double)(statistics.TotalPresentCount + statistics.TotalLateCount) / totalAttendances * 100, 2) 
            : 0;
        var attendancesByWeek = allAttendances
            .GroupBy(a => group.Lessons.FirstOrDefault(l => l.Id == a.LessonId)?.WeekIndex ?? 0)
            .ToDictionary(g => g.Key, g => g.ToList());
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
    #endregion


}