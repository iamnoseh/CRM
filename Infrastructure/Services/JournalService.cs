using System.Net;
using Domain.DTOs.Journal;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class JournalService(DataContext context) : IJournalService
{
    public async Task<Response<string>> GenerateWeeklyJournalAsync(int groupId, int weekNumber)
    {
        try
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Гурӯҳ ёфт нашуд");

            var existing = await context.Journals
                .FirstOrDefaultAsync(j => j.GroupId == groupId && j.WeekNumber == weekNumber && !j.IsDeleted);
            if (existing != null)
                return new Response<string>(HttpStatusCode.OK, "Журнал аллакай барои ин ҳафта вуҷуд дорад");

            var weekStart = GetWeekStart(group.StartDate.UtcDateTime, weekNumber);
            var weekEnd = weekStart.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);

            var journal = new Journal
            {
                GroupId = groupId,
                WeekNumber = weekNumber,
                WeekStartDate = weekStart,
                WeekEndDate = weekEnd,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await context.Journals.AddAsync(journal);
            await context.SaveChangesAsync();

            var lessonDays = ParseLessonDays(group.LessonDays);
            if (lessonDays.Count == 0)
            {
                lessonDays = new List<int> { 1, 2, 3, 4, 5 };
            }

            var students = await context.StudentGroups
                .Include(sg => sg.Student)
                .Where(sg => sg.GroupId == groupId && sg.IsActive && !sg.IsDeleted && !sg.Student!.IsDeleted)
                .Select(sg => sg.Student!)
                .ToListAsync();

            for (int i = 0; i < 6; i++)
            {
                var lessonNumber = i + 1; // 1..6
                var dayIdxZeroBased = lessonDays[i % lessonDays.Count]; // 0..6 (Sunday..Saturday)
                var desiredDay = (DayOfWeek)dayIdxZeroBased;

                var weekStartDate = journal.WeekStartDate.UtcDateTime.Date;
                var slotDate = Enumerable.Range(0, 7)
                    .Select(offset => weekStartDate.AddDays(offset))
                    .First(d => d.DayOfWeek == desiredDay);

                var dayOfWeekOneBased = dayIdxZeroBased % 7 + 1; // 1..7

                var lessonType = group.HasWeeklyExam && lessonNumber == 6
                    ? LessonType.Exam
                    : LessonType.Regular;

                foreach (var student in students)
                {
                    var entry = new JournalEntry
                    {
                        JournalId = journal.Id,
                        StudentId = student.Id,
                        DayOfWeek = dayOfWeekOneBased,
                        LessonNumber = lessonNumber,
                        LessonType = lessonType,
                        StartTime = group.LessonStartTime,
                        EndTime = group.LessonEndTime,
                        AttendanceStatus = AttendanceStatus.Absent,
                        EntryDate = DateTime.SpecifyKind(slotDate, DateTimeKind.Utc)
                    };
                    await context.JournalEntries.AddAsync(entry);
                }
            }

            await context.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.Created, "Журнали ҳафтавӣ эҷод шуд");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<GetJournalDto>> GetJournalAsync(int groupId, int weekNumber)
    {
        try
        {
            var journal = await context.Journals
                .Include(j => j.Group)
                .Include(j => j.Entries)
                .FirstOrDefaultAsync(j => j.GroupId == groupId && j.WeekNumber == weekNumber && !j.IsDeleted);

            if (journal == null)
                return new Response<GetJournalDto>(HttpStatusCode.NotFound, "Журнал ёфт нашуд");

            var studentIds = journal.Entries.Select(e => e.StudentId).Distinct().ToList();
            var students = await context.Students
                .Where(s => studentIds.Contains(s.Id) && !s.IsDeleted)
                .Select(s => new { s.Id, s.FullName})
                .ToListAsync();

            var progresses = students.Select(s => new StudentProgress
            {
                StudentId = s.Id,
                StudentName = $"{s.FullName}".Trim(),
                StudentEntries = journal.Entries
                    .Where(e => e.StudentId == s.Id)
                    .OrderBy(e => e.LessonNumber)
                    .ThenBy(e => e.DayOfWeek)
                    .Select(e => new GetJournalEntryDto
                    {
                        Id = e.Id,
                        DayOfWeek = e.DayOfWeek,
                        LessonNumber = e.LessonNumber,
                        LessonType = e.LessonType,
                        Grade = e.Grade,
                        BonusPoints = e.BonusPoints,
                        AttendanceStatus = e.AttendanceStatus,
                        Comment = e.Comment,
                        CommentCategory = e.CommentCategory,
                        EntryDate = e.EntryDate,
                        StartTime = e.StartTime,
                        EndTime = e.EndTime
                    }).ToList()
            }).ToList();

            var dto = new GetJournalDto
            {
                Id = journal.Id,
                GroupId = journal.GroupId,
                GroupName = journal.Group?.Name,
                WeekNumber = journal.WeekNumber,
                WeekStartDate = journal.WeekStartDate,
                WeekEndDate = journal.WeekEndDate,
                Progresses = progresses
            };

            return new Response<GetJournalDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetJournalDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<string>> UpdateEntryAsync(int entryId, UpdateJournalEntryDto request)
    {
        try
        {
            var entry = await context.JournalEntries.FirstOrDefaultAsync(e => e.Id == entryId && !e.IsDeleted);
            if (entry == null)
                return new Response<string>(HttpStatusCode.NotFound, "Элемент ёфт нашуд");

            if (request.Grade.HasValue) entry.Grade = request.Grade.Value;
            if (request.BonusPoints.HasValue) entry.BonusPoints = request.BonusPoints.Value;
            if (request.AttendanceStatus.HasValue) entry.AttendanceStatus = request.AttendanceStatus.Value;
            if (!string.IsNullOrWhiteSpace(request.Comment)) entry.Comment = request.Comment;
            if (request.CommentCategory.HasValue) entry.CommentCategory = request.CommentCategory.Value;

            entry.UpdatedAt = DateTimeOffset.UtcNow;
            context.JournalEntries.Update(entry);
            await context.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, "Навсозӣ шуд");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private static DateTimeOffset GetWeekStart(DateTime groupStart, int weekNumber)
    {
        var startUtc = DateTime.SpecifyKind(groupStart.Date, DateTimeKind.Utc);
        return new DateTimeOffset(startUtc.AddDays((weekNumber - 1) * 7), TimeSpan.Zero);
    }

    private static List<int> ParseLessonDays(string? lessonDays)
    {
        if (string.IsNullOrWhiteSpace(lessonDays)) return new List<int>();
        return lessonDays.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim())
            .Where(d => int.TryParse(d, out var v) && v >= 0 && v <= 6)
            .Select(int.Parse)
            .Distinct()
            .ToList();
    }
}


