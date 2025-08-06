using System.Net;
using Domain.DTOs.Journal;
using Domain.Entities;
using Domain.Enums;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class JournalService : IJournalService
{
    private readonly DataContext _context;

    public JournalService(DataContext context)
    {
        _context = context;
    }

    public async Task<Response<GetJournalEntryDto>> CreateEntryAsync(CreateJournalEntryDto dto)
    {
        try
        {
            var entry = new JournalEntry
            {
                GroupId = dto.GroupId,
                StudentId = dto.StudentId,
                LessonId = dto.LessonId,
                ExamId = dto.ExamId,
                EntryDate = dto.EntryDate,
                WeekIndex = dto.WeekIndex,
                DayIndex = dto.DayIndex,
                Grade = dto.Grade,
                BonusPoints = dto.BonusPoints,
                AttendanceStatus = dto.AttendanceStatus,
                Comment = dto.Comment,
                CommentType = dto.CommentType,
                EntryType = dto.EntryType,
                CreatedBy = "System" // TODO: Get from current user
            };

            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();

            return await GetEntryByIdAsync(entry.Id);
        }
        catch (Exception ex)
        {
            return new Response<GetJournalEntryDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error creating journal entry: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetJournalEntryDto>> UpdateEntryAsync(UpdateJournalEntryDto dto)
    {
        try
        {
            var entry = await _context.JournalEntries.FindAsync(dto.Id);
            if (entry == null)
            {
                return new Response<GetJournalEntryDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Journal entry not found"
                };
            }

            if (dto.Grade.HasValue) entry.Grade = dto.Grade.Value;
            if (dto.BonusPoints.HasValue) entry.BonusPoints = dto.BonusPoints.Value;
            if (dto.AttendanceStatus.HasValue) entry.AttendanceStatus = dto.AttendanceStatus.Value;
            if (!string.IsNullOrEmpty(dto.Comment)) entry.Comment = dto.Comment;
            if (dto.CommentType.HasValue) entry.CommentType = dto.CommentType.Value;

            entry.LastModifiedBy = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetEntryByIdAsync(entry.Id);
        }
        catch (Exception ex)
        {
            return new Response<GetJournalEntryDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error updating journal entry: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetJournalEntryDto>> GetEntryByIdAsync(int id)
    {
        try
        {
            var entry = await _context.JournalEntries
                .Include(e => e.Group)
                .Include(e => e.Student)
                .Include(e => e.Lesson)
                .Include(e => e.Exam)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (entry == null)
            {
                return new Response<GetJournalEntryDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Journal entry not found"
                };
            }

            var dto = new GetJournalEntryDto
            {
                Id = entry.Id,
                GroupId = entry.GroupId,
                GroupName = entry.Group?.Name ?? "",
                StudentId = entry.StudentId,
                StudentFullName = entry.Student?.FullName ?? "",
                LessonId = entry.LessonId,
                LessonStartTime = entry.Lesson.StartTime.DateTime,
                ExamId = entry.ExamId,
                ExamDescription = entry.Exam?.Description,
                ExamType = entry.Exam?.ExamType,
                EntryDate = entry.EntryDate,
                WeekIndex = entry.WeekIndex,
                DayIndex = entry.DayIndex,
                Grade = entry.Grade,
                BonusPoints = entry.BonusPoints,
                AttendanceStatus = entry.AttendanceStatus,
                Comment = entry.Comment,
                CommentType = entry.CommentType,
                EntryType = entry.EntryType,
                CreatedBy = entry.CreatedBy,
                CreatedAt = entry.CreatedAt,
                UpdatedAt = entry.UpdatedAt
            };

            return new Response<GetJournalEntryDto>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = dto
            };
        }
        catch (Exception ex)
        {
            return new Response<GetJournalEntryDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error getting journal entry: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetJournalEntryDto>> DeleteEntryAsync(int id)
    {
        try
        {
            var entry = await _context.JournalEntries.FindAsync(id);
            if (entry == null)
            {
                return new Response<GetJournalEntryDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Journal entry not found"
                };
            }

            _context.JournalEntries.Remove(entry);
            await _context.SaveChangesAsync();

            return new Response<GetJournalEntryDto>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Journal entry deleted successfully"
            };
        }
        catch (Exception ex)
        {
            return new Response<GetJournalEntryDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error deleting journal entry: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetGroupJournalDto>> GetGroupJournalAsync(int groupId, DateTime date)
    {
        try
        {
            var group = await _context.Groups
                .Include(g => g.StudentGroups)
                .ThenInclude(sg => sg.Student)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                return new Response<GetGroupJournalDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Group not found"
                };
            }

            var weekIndex = GetWeekIndex(date, group.StartDate.DateTime);
            var dayIndex = GetDayIndex(date, group.StartDate.DateTime);

            var entries = await _context.JournalEntries
                .Include(e => e.Student)
                .Include(e => e.Lesson)
                .Include(e => e.Exam)
                .Where(e => e.GroupId == groupId && e.EntryDate.Date == date.Date)
                .ToListAsync();

            var exam = await _context.Exams
                .FirstOrDefaultAsync(e => e.GroupId == groupId && e.ExamDate.Date == date.Date);

            var dto = new GetGroupJournalDto
            {
                GroupId = groupId,
                GroupName = group.Name,
                Date = date,
                WeekIndex = weekIndex,
                DayIndex = dayIndex,
                Entries = entries.Select(e => new GetJournalEntryDto
                {
                    Id = e.Id,
                    GroupId = e.GroupId,
                    GroupName = group.Name,
                    StudentId = e.StudentId,
                    StudentFullName = e.Student?.FullName ?? "",
                    LessonId = e.LessonId,
                    LessonStartTime = e.Lesson?.StartTime.DateTime,
                    ExamId = e.ExamId,
                    ExamDescription = e.Exam?.Description,
                    ExamType = e.Exam?.ExamType,
                    EntryDate = e.EntryDate,
                    WeekIndex = e.WeekIndex,
                    DayIndex = e.DayIndex,
                    Grade = e.Grade,
                    BonusPoints = e.BonusPoints,
                    AttendanceStatus = e.AttendanceStatus,
                    Comment = e.Comment,
                    CommentType = e.CommentType,
                    EntryType = e.EntryType,
                    CreatedBy = e.CreatedBy,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                }).ToList(),
                TotalStudents = group.StudentGroups.Count(sg => sg.IsActive.Value),
                PresentStudents = entries.Count(e => e.AttendanceStatus == AttendanceStatus.Present),
                AbsentStudents = entries.Count(e => e.AttendanceStatus == AttendanceStatus.Absent),
                LateStudents = entries.Count(e => e.AttendanceStatus == AttendanceStatus.Late),
                AverageGrade = entries.Where(e => e.Grade.HasValue).Any() 
                    ? entries.Where(e => e.Grade.HasValue).Average(e => e.Grade.Value) 
                    : 0,
                TotalBonusPoints = entries.Where(e => e.BonusPoints.HasValue).Sum(e => e.BonusPoints.Value),
                HasExam = exam != null,
                ExamType = exam?.ExamType,
                ExamDescription = exam?.Description
            };

            return new Response<GetGroupJournalDto>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = dto
            };
        }
        catch (Exception ex)
        {
            return new Response<GetGroupJournalDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error getting group journal: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetGroupJournalDto>> GetGroupJournalByWeekAsync(int groupId, int weekIndex)
    {
        try
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
            {
                return new Response<GetGroupJournalDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Group not found"
                };
            }

            var startDate = group.StartDate.AddDays((weekIndex - 1) * 7);
            var endDate = startDate.AddDays(6);

            var entries = await _context.JournalEntries
                .Include(e => e.Student)
                .Include(e => e.Lesson)
                .Include(e => e.Exam)
                .Where(e => e.GroupId == groupId && e.WeekIndex == weekIndex)
                .ToListAsync();

            var dto = new GetGroupJournalDto
            {
                GroupId = groupId,
                GroupName = group.Name,
                Date = startDate.DateTime,
                WeekIndex = weekIndex,
                DayIndex = 1,
                Entries = entries.Select(e => new GetJournalEntryDto
                {
                    Id = e.Id,
                    GroupId = e.GroupId,
                    GroupName = group.Name,
                    StudentId = e.StudentId,
                    StudentFullName = e.Student?.FullName ?? "",
                    LessonId = e.LessonId,
                    LessonStartTime = e.Lesson?.StartTime.DateTime,
                    ExamId = e.ExamId,
                    ExamDescription = e.Exam?.Description,
                    ExamType = e.Exam?.ExamType,
                    EntryDate = e.EntryDate,
                    WeekIndex = e.WeekIndex,
                    DayIndex = e.DayIndex,
                    Grade = e.Grade,
                    BonusPoints = e.BonusPoints,
                    AttendanceStatus = e.AttendanceStatus,
                    Comment = e.Comment,
                    CommentType = e.CommentType,
                    EntryType = e.EntryType,
                    CreatedBy = e.CreatedBy,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                }).ToList()
            };

            return new Response<GetGroupJournalDto>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = dto
            };
        }
        catch (Exception ex)
        {
            return new Response<GetGroupJournalDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error getting group journal by week: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetJournalEntryDto>>> GetGroupJournalByDateRangeAsync(int groupId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var entries = await _context.JournalEntries
                .Include(e => e.Group)
                .Include(e => e.Student)
                .Include(e => e.Lesson)
                .Include(e => e.Exam)
                .Where(e => e.GroupId == groupId && e.EntryDate >= startDate && e.EntryDate <= endDate)
                .ToListAsync();

            var dtos = entries.Select(e => new GetJournalEntryDto
            {
                Id = e.Id,
                GroupId = e.GroupId,
                GroupName = e.Group?.Name ?? "",
                StudentId = e.StudentId,
                StudentFullName = e.Student?.FullName ?? "",
                LessonId = e.LessonId,
                LessonStartTime = e.Lesson?.StartTime.DateTime,
                ExamId = e.ExamId,
                ExamDescription = e.Exam?.Description,
                ExamType = e.Exam?.ExamType,
                EntryDate = e.EntryDate,
                WeekIndex = e.WeekIndex,
                DayIndex = e.DayIndex,
                Grade = e.Grade,
                BonusPoints = e.BonusPoints,
                AttendanceStatus = e.AttendanceStatus,
                Comment = e.Comment,
                CommentType = e.CommentType,
                EntryType = e.EntryType,
                CreatedBy = e.CreatedBy,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            }).ToList();

            return new Response<List<GetJournalEntryDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetJournalEntryDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error getting group journal by date range: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetJournalEntryDto>>> GetStudentJournalAsync(int studentId, int groupId)
    {
        try
        {
            var entries = await _context.JournalEntries
                .Include(e => e.Group)
                .Include(e => e.Lesson)
                .Include(e => e.Exam)
                .Where(e => e.StudentId == studentId && e.GroupId == groupId)
                .OrderBy(e => e.EntryDate)
                .ToListAsync();

            var dtos = entries.Select(e => new GetJournalEntryDto
            {
                Id = e.Id,
                GroupId = e.GroupId,
                GroupName = e.Group?.Name ?? "",
                StudentId = e.StudentId,
                StudentFullName = e.Student.FullName,
                LessonId = e.LessonId,
                LessonStartTime = e.Lesson?.StartTime.DateTime,
                ExamId = e.ExamId,
                ExamDescription = e.Exam?.Description,
                ExamType = e.Exam?.ExamType,
                EntryDate = e.EntryDate,
                WeekIndex = e.WeekIndex,
                DayIndex = e.DayIndex,
                Grade = e.Grade,
                BonusPoints = e.BonusPoints,
                AttendanceStatus = e.AttendanceStatus,
                Comment = e.Comment,
                CommentType = e.CommentType,
                EntryType = e.EntryType,
                CreatedBy = e.CreatedBy,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            }).ToList();

            return new Response<List<GetJournalEntryDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetJournalEntryDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error getting student journal: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetJournalEntryDto>>> GetStudentJournalByDateRangeAsync(int studentId, int groupId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var entries = await _context.JournalEntries
                .Include(e => e.Group)
                .Include(e => e.Lesson)
                .Include(e => e.Exam)
                .Where(e => e.StudentId == studentId && e.GroupId == groupId && 
                           e.EntryDate >= startDate && e.EntryDate <= endDate)
                .OrderBy(e => e.EntryDate)
                .ToListAsync();

            var dtos = entries.Select(e => new GetJournalEntryDto
            {
                Id = e.Id,
                GroupId = e.GroupId,
                GroupName = e.Group?.Name ?? "",
                StudentId = e.StudentId,
                StudentFullName = e.Student.FullName,
                LessonId = e.LessonId,
                LessonStartTime = e.Lesson?.StartTime.DateTime,
                ExamId = e.ExamId,
                ExamDescription = e.Exam?.Description,
                ExamType = e.Exam?.ExamType,
                EntryDate = e.EntryDate,
                WeekIndex = e.WeekIndex,
                DayIndex = e.DayIndex,
                Grade = e.Grade,
                BonusPoints = e.BonusPoints,
                AttendanceStatus = e.AttendanceStatus,
                Comment = e.Comment,
                CommentType = e.CommentType,
                EntryType = e.EntryType,
                CreatedBy = e.CreatedBy,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            }).ToList();

            return new Response<List<GetJournalEntryDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetJournalEntryDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error getting student journal by date range: {ex.Message}"
            };
        }
    }

    public async Task<Response<List<GetJournalEntryDto>>> GetEntriesByFilterAsync(JournalFilter filter)
    {
        try
        {
            var query = _context.JournalEntries
                .Include(e => e.Group)
                .Include(e => e.Student)
                .Include(e => e.Lesson)
                .Include(e => e.Exam)
                .AsQueryable();

            if (filter.GroupId.HasValue)
                query = query.Where(e => e.GroupId == filter.GroupId.Value);

            if (filter.StudentId.HasValue)
                query = query.Where(e => e.StudentId == filter.StudentId.Value);

            if (filter.LessonId.HasValue)
                query = query.Where(e => e.LessonId == filter.LessonId.Value);

            if (filter.ExamId.HasValue)
                query = query.Where(e => e.ExamId == filter.ExamId.Value);

            if (filter.StartDate.HasValue)
                query = query.Where(e => e.EntryDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(e => e.EntryDate <= filter.EndDate.Value);

            if (filter.WeekIndex.HasValue)
                query = query.Where(e => e.WeekIndex == filter.WeekIndex.Value);

            if (filter.DayIndex.HasValue)
                query = query.Where(e => e.DayIndex == filter.DayIndex.Value);

            if (filter.EntryType.HasValue)
                query = query.Where(e => e.EntryType == filter.EntryType.Value);

            if (filter.AttendanceStatus.HasValue)
                query = query.Where(e => e.AttendanceStatus == filter.AttendanceStatus.Value);

            if (filter.CommentType.HasValue)
                query = query.Where(e => e.CommentType == filter.CommentType.Value);

            if (filter.MinGrade.HasValue)
                query = query.Where(e => e.Grade >= filter.MinGrade.Value);

            if (filter.MaxGrade.HasValue)
                query = query.Where(e => e.Grade <= filter.MaxGrade.Value);

            if (filter.HasGrade.HasValue)
            {
                if (filter.HasGrade.Value)
                    query = query.Where(e => e.Grade.HasValue);
                else
                    query = query.Where(e => !e.Grade.HasValue);
            }

            if (filter.HasComment.HasValue)
            {
                if (filter.HasComment.Value)
                    query = query.Where(e => !string.IsNullOrEmpty(e.Comment));
                else
                    query = query.Where(e => string.IsNullOrEmpty(e.Comment));
            }

            if (filter.HasBonus.HasValue)
            {
                if (filter.HasBonus.Value)
                    query = query.Where(e => e.BonusPoints.HasValue);
                else
                    query = query.Where(e => !e.BonusPoints.HasValue);
            }

            var entries = await query.ToListAsync();

            var dtos = entries.Select(e => new GetJournalEntryDto
            {
                Id = e.Id,
                GroupId = e.GroupId,
                GroupName = e.Group?.Name ?? "",
                StudentId = e.StudentId,
                StudentFullName = e.Student?.FullName ?? "",
                LessonId = e.LessonId,
                LessonStartTime = e.Lesson?.StartTime.DateTime,
                ExamId = e.ExamId,
                ExamDescription = e.Exam?.Description,
                ExamType = e.Exam?.ExamType,
                EntryDate = e.EntryDate,
                WeekIndex = e.WeekIndex,
                DayIndex = e.DayIndex,
                Grade = e.Grade,
                BonusPoints = e.BonusPoints,
                AttendanceStatus = e.AttendanceStatus,
                Comment = e.Comment,
                CommentType = e.CommentType,
                EntryType = e.EntryType,
                CreatedBy = e.CreatedBy,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            }).ToList();

            return new Response<List<GetJournalEntryDto>>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            return new Response<List<GetJournalEntryDto>>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error getting entries by filter: {ex.Message}"
            };
        }
    }

    public async Task<Response<PaginationResponse<GetJournalEntryDto>>> GetEntriesPaginatedAsync(JournalFilter filter, int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    // public async Task<Response<PaginationResponse<GetJournalEntryDto>>> GetEntriesPaginatedAsync(JournalFilter filter, int page, int pageSize)
    // {
    //     try
    //     {
    //         var query = _context.JournalEntries
    //             .Include(e => e.Group)
    //             .Include(e => e.Student)
    //             .Include(e => e.Lesson)
    //             .Include(e => e.Exam)
    //             .AsQueryable();
    //
    //         if (filter.GroupId.HasValue)
    //             query = query.Where(e => e.GroupId == filter.GroupId.Value);
    //
    //         if (filter.StudentId.HasValue)
    //             query = query.Where(e => e.StudentId == filter.StudentId.Value);
    //
    //         if (filter.LessonId.HasValue)
    //             query = query.Where(e => e.LessonId == filter.LessonId.Value);
    //
    //         if (filter.ExamId.HasValue)
    //             query = query.Where(e => e.ExamId == filter.ExamId.Value);
    //
    //         if (filter.StartDate.HasValue)
    //             query = query.Where(e => e.EntryDate >= filter.StartDate.Value);
    //
    //         if (filter.EndDate.HasValue)
    //             query = query.Where(e => e.EntryDate <= filter.EndDate.Value);
    //
    //         if (filter.WeekIndex.HasValue)
    //             query = query.Where(e => e.WeekIndex == filter.WeekIndex.Value);
    //
    //         if (filter.DayIndex.HasValue)
    //             query = query.Where(e => e.DayIndex == filter.DayIndex.Value);
    //
    //         if (filter.EntryType.HasValue)
    //             query = query.Where(e => e.EntryType == filter.EntryType.Value);
    //
    //         if (filter.AttendanceStatus.HasValue)
    //             query = query.Where(e => e.AttendanceStatus == filter.AttendanceStatus.Value);
    //
    //         if (filter.CommentType.HasValue)
    //             query = query.Where(e => e.CommentType == filter.CommentType.Value);
    //
    //         if (filter.MinGrade.HasValue)
    //             query = query.Where(e => e.Grade >= filter.MinGrade.Value);
    //
    //         if (filter.MaxGrade.HasValue)
    //             query = query.Where(e => e.Grade <= filter.MaxGrade.Value);
    //
    //         if (filter.HasGrade.HasValue)
    //         {
    //             if (filter.HasGrade.Value)
    //                 query = query.Where(e => e.Grade.HasValue);
    //             else
    //                 query = query.Where(e => !e.Grade.HasValue);
    //         }
    //
    //         if (filter.HasComment.HasValue)
    //         {
    //             if (filter.HasComment.Value)
    //                 query = query.Where(e => !string.IsNullOrEmpty(e.Comment));
    //             else
    //                 query = query.Where(e => string.IsNullOrEmpty(e.Comment));
    //         }
    //
    //         if (filter.HasBonus.HasValue)
    //         {
    //             if (filter.HasBonus.Value)
    //                 query = query.Where(e => e.BonusPoints.HasValue);
    //             else
    //                 query = query.Where(e => !e.BonusPoints.HasValue);
    //         }
    //
    //         var totalCount = await query.CountAsync();
    //         var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
    //
    //         var entries = await query
    //             .Skip((page - 1) * pageSize)
    //             .Take(pageSize)
    //             .ToListAsync();
    //
    //         var dtos = entries.Select(e => new GetJournalEntryDto
    //         {
    //             Id = e.Id,
    //             GroupId = e.GroupId,
    //             GroupName = e.Group?.Name ?? "",
    //             StudentId = e.StudentId,
    //             StudentFullName = e.Student?.FullName ?? "",
    //             LessonId = e.LessonId,
    //             LessonStartTime = e.Lesson?.StartTime.DateTime,
    //             ExamId = e.ExamId,
    //             ExamDescription = e.Exam?.Description,
    //             ExamType = e.Exam?.ExamType,
    //             EntryDate = e.EntryDate,
    //             WeekIndex = e.WeekIndex,
    //             DayIndex = e.DayIndex,
    //             Grade = e.Grade,
    //             BonusPoints = e.BonusPoints,
    //             AttendanceStatus = e.AttendanceStatus,
    //             Comment = e.Comment,
    //             CommentType = e.CommentType,
    //             EntryType = e.EntryType,
    //             CreatedBy = e.CreatedBy,
    //             CreatedAt = e.CreatedAt,
    //             UpdatedAt = e.UpdatedAt
    //         }).ToList();
    //
    //         var paginationResponse = new PaginationResponse<GetJournalEntryDto>(dtos, totalCount, page, pageSize);
    //
    //         return new Response<PaginationResponse<GetJournalEntryDto>>
    //         {
    //             StatusCode = (int)HttpStatusCode.OK,
    //             Data = paginationResponse
    //         };
    //     }
    //     catch (Exception ex)
    //     {
    //         return new Response<PaginationResponse<GetJournalEntryDto>>
    //         {
    //             StatusCode = (int)HttpStatusCode.InternalServerError,
    //             Message = $"Error getting paginated entries: {ex.Message}"
    //         };
    //     }
    // }

    public async Task<Response<bool>> CreateDailyEntriesAsync(int groupId, DateTime date)
    {
        try
        {
            var group = await _context.Groups
                .Include(g => g.StudentGroups.Where(sg => sg.IsActive.Value))
                .ThenInclude(sg => sg.Student)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                return new Response<bool>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Group not found"
                };
            }

            var weekIndex = GetWeekIndex(date, group.StartDate.DateTime);
            var dayIndex = GetDayIndex(date, group.StartDate.DateTime);

            var existingEntries = await _context.JournalEntries
                .Where(e => e.GroupId == groupId && e.EntryDate.Date == date.Date)
                .ToListAsync();

            if (existingEntries.Any())
            {
                return new Response<bool>
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Data = true,
                    Message = "Entries already exist for this date"
                };
            }

            var entries = new List<JournalEntry>();

            foreach (var studentGroup in group.StudentGroups)
            {
                entries.Add(new JournalEntry
                {
                    GroupId = groupId,
                    StudentId = studentGroup.StudentId,
                    EntryDate = date,
                    WeekIndex = weekIndex,
                    DayIndex = dayIndex,
                    AttendanceStatus = AttendanceStatus.Absent, 
                    EntryType = JournalEntryType.Attendance,
                    CreatedBy = "System"
                });
                entries.Add(new JournalEntry
                {
                    GroupId = groupId,
                    StudentId = studentGroup.StudentId,
                    EntryDate = date,
                    WeekIndex = weekIndex,
                    DayIndex = dayIndex,
                    EntryType = JournalEntryType.LessonGrade,
                    CreatedBy = "System"
                });
            }

            _context.JournalEntries.AddRange(entries);
            await _context.SaveChangesAsync();

            return new Response<bool>
            {
                StatusCode = (int)HttpStatusCode.Created,
                Data = true,
                Message = $"Created {entries.Count} journal entries for {date:yyyy-MM-dd}"
            };
        }
        catch (Exception ex)
        {
            return new Response<bool>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error creating daily entries: {ex.Message}"
            };
        }
    }

    public async Task<Response<bool>> CreateExamEntriesAsync(int examId)
    {
        try
        {
            var exam = await _context.Exams
                .Include(e => e.Group)
                .ThenInclude(g => g.StudentGroups.Where(sg => sg.IsActive.Value))
                .ThenInclude(sg => sg.Student)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
            {
                return new Response<bool>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Exam not found"
                };
            }

            var weekIndex = exam.WeekIndex;
            var dayIndex = GetDayIndex(exam.ExamDate, exam.Group.StartDate.DateTime);

            var existingEntries = await _context.JournalEntries
                .Where(e => e.ExamId == examId)
                .ToListAsync();

            if (existingEntries.Any())
            {
                return new Response<bool>
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Data = true,
                    Message = "Exam entries already exist"
                };
            }

            var entries = new List<JournalEntry>();

            foreach (var studentGroup in exam.Group.StudentGroups)
            {
                entries.Add(new JournalEntry
                {
                    GroupId = exam.GroupId,
                    StudentId = studentGroup.StudentId,
                    ExamId = examId,
                    EntryDate = exam.ExamDate,
                    WeekIndex = weekIndex,
                    DayIndex = dayIndex,
                    EntryType = JournalEntryType.ExamGrade,
                    CreatedBy = "System"
                });
            }

            _context.JournalEntries.AddRange(entries);
            await _context.SaveChangesAsync();

            return new Response<bool>
            {
                StatusCode = (int)HttpStatusCode.Created,
                Data = true,
                Message = $"Created {entries.Count} exam entries for exam {examId}"
            };
        }
        catch (Exception ex)
        {
            return new Response<bool>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error creating exam entries: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetJournalEntryDto>> UpdateGradeAsync(int entryId, decimal grade, decimal? bonus)
    {
        try
        {
            var entry = await _context.JournalEntries.FindAsync(entryId);
            if (entry == null)
            {
                return new Response<GetJournalEntryDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Journal entry not found"
                };
            }

            entry.Grade = grade;
            entry.BonusPoints = bonus;
            entry.LastModifiedBy = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetEntryByIdAsync(entryId);
        }
        catch (Exception ex)
        {
            return new Response<GetJournalEntryDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error updating grade: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetJournalEntryDto>> UpdateAttendanceAsync(int entryId, AttendanceStatus status)
    {
        try
        {
            var entry = await _context.JournalEntries.FindAsync(entryId);
            if (entry == null)
            {
                return new Response<GetJournalEntryDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Journal entry not found"
                };
            }

            entry.AttendanceStatus = status;
            entry.LastModifiedBy = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetEntryByIdAsync(entryId);
        }
        catch (Exception ex)
        {
            return new Response<GetJournalEntryDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error updating attendance: {ex.Message}"
            };
        }
    }

    public async Task<Response<GetJournalEntryDto>> AddCommentAsync(int entryId, string comment, CommentType type)
    {
        try
        {
            var entry = await _context.JournalEntries.FindAsync(entryId);
            if (entry == null)
            {
                return new Response<GetJournalEntryDto>
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Journal entry not found"
                };
            }

            entry.Comment = comment;
            entry.CommentType = type;
            entry.LastModifiedBy = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetEntryByIdAsync(entryId);
        }
        catch (Exception ex)
        {
            return new Response<GetJournalEntryDto>
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = $"Error adding comment: {ex.Message}"
            };
        }
    }

    private int GetWeekIndex(DateTime date, DateTime startDate)
    {
        var daysDiff = (date - startDate).Days;
        return (daysDiff / 7) + 1;
    }

    private int GetDayIndex(DateTime date, DateTime startDate)
    {
        var daysDiff = (date - startDate).Days;
        return daysDiff + 1;
    }

    private JournalEntryType DetermineEntryType(JournalEntry entry)
    {
        if (entry.Grade.HasValue && entry.ExamId.HasValue)
            return JournalEntryType.ExamGrade;
        else if (entry.Grade.HasValue && entry.LessonId.HasValue)
            return JournalEntryType.LessonGrade;
        else if (entry.AttendanceStatus.HasValue)
            return JournalEntryType.Attendance;
        else if (!string.IsNullOrEmpty(entry.Comment))
            return JournalEntryType.Comment;
        else if (entry.BonusPoints.HasValue && entry.BonusPoints > 0)
            return JournalEntryType.Bonus;
        else
            return JournalEntryType.LessonGrade; 
    }
} 