using System.Net;
using Domain.DTOs.Journal;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Infrastructure.Helpers;
using System.Security.Claims;

namespace Infrastructure.Services;

public class JournalService(DataContext context, IHttpContextAccessor httpContextAccessor) : IJournalService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    public async Task<Response<string>> GenerateWeeklyJournalAsync(int groupId, int weekNumber)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            var groupQuery = context.Groups
                .Include(g => g.Course)
                .Where(g => !g.IsDeleted)
                .AsQueryable();
            if (centerId != null)
            {
                groupQuery = groupQuery.Where(g => g.Course!.CenterId == centerId);
            }
            var group = await groupQuery.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Гурӯҳ ёфт нашуд");

            var existingWeeks = await context.Journals
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .Select(j => j.WeekNumber)
                .ToListAsync();
            var maxExistingWeek = existingWeeks.Count == 0 ? 0 : existingWeeks.Max();
            var expectedNextWeek = maxExistingWeek + 1;

            if (weekNumber > group.TotalWeeks)
                return new Response<string>(HttpStatusCode.BadRequest,
                    $"Шумо наметавонед ҳафтаи {weekNumber}-ро созед. Шумораи умумии ҳафтаҳо: {group.TotalWeeks}. Ҳафтаи навбатӣ: {expectedNextWeek}.");

            if (weekNumber <= maxExistingWeek)
                return new Response<string>(HttpStatusCode.BadRequest,
                    $"Ин ҳафта аллакай сохта шудааст ё аз ҳафтаҳои гузашта мебошад. Лутфан ҳафтаи {expectedNextWeek}-ро созед.");

            if (weekNumber != expectedNextWeek)
                return new Response<string>(HttpStatusCode.BadRequest,
                    $"Пайдарпайӣ риоя нашудааст. Лутфан аввал ҳафтаи {expectedNextWeek}-ро созед.");

            var existing = await context.Journals
                .FirstOrDefaultAsync(j => j.GroupId == groupId && j.WeekNumber == weekNumber && !j.IsDeleted);
            if (existing != null)
                return new Response<string>(HttpStatusCode.OK, "Журнал аллакай барои ин ҳафта вуҷуд дорад");

            var lessonDays = ParseLessonDays(group.LessonDays);
            if (lessonDays.Count == 0)
            {
                lessonDays = new List<int> { 2, 3, 4, 5, 6 };
            }

            var targetLessons = 6;
            DateTime cursor;
            if (weekNumber == 1)
            {
                cursor = group.StartDate.UtcDateTime.Date;
            }
            else
            {
                var prevJournal = await context.Journals
                    .Where(j => j.GroupId == groupId && j.WeekNumber == weekNumber - 1 && !j.IsDeleted)
                    .OrderByDescending(j => j.Id)
                    .FirstOrDefaultAsync();
                cursor = prevJournal != null
                    ? prevJournal.WeekEndDate.UtcDateTime.Date.AddDays(1)
                    : group.StartDate.UtcDateTime.Date.AddDays((weekNumber - 1) * 7);
            }

            var plannedSlots = new List<(DateTime date, int dayOfWeekOneBased, int lessonNumber)>();
            while (plannedSlots.Count < targetLessons)
            {
                var dotNetDayOfWeek = (int)cursor.DayOfWeek;
                var crmDayOfWeek = ConvertDotNetToCrmDayOfWeek(dotNetDayOfWeek);
                
                if (lessonDays.Contains(crmDayOfWeek))
                {
                    plannedSlots.Add((cursor, crmDayOfWeek, plannedSlots.Count + 1));
                }

                cursor = cursor.AddDays(1);
            }

            var firstSlotDate = plannedSlots.First().date;
            var lastSlotDate = plannedSlots.Last().date;
            var weekStart = new DateTimeOffset(firstSlotDate.Year, firstSlotDate.Month, firstSlotDate.Day, 0, 0, 0,
                TimeSpan.Zero);
            var weekEnd = new DateTimeOffset(lastSlotDate.Year, lastSlotDate.Month, lastSlotDate.Day, 23, 59, 59,
                TimeSpan.Zero);

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

            var students = await context.StudentGroups
                .Include(sg => sg.Student)
                .Where(sg => sg.GroupId == groupId && sg.IsActive && !sg.IsDeleted && !sg.Student!.IsDeleted)
                .Select(sg => sg.Student!)
                .ToListAsync();

            foreach (var slot in plannedSlots)
            {
                
                var lessonType = DetermineLessonType(group.HasWeeklyExam, weekNumber, slot.lessonNumber, targetLessons);
                
                foreach (var student in students)
                {
                    var entry = new JournalEntry
                    {
                        JournalId = journal.Id,
                        StudentId = student.Id,
                        DayOfWeek = slot.dayOfWeekOneBased,
                        LessonNumber = slot.lessonNumber,
                        LessonType = lessonType,
                        StartTime = group.LessonStartTime,
                        EndTime = group.LessonEndTime,
                        AttendanceStatus = AttendanceStatus.Absent,
                        EntryDate = DateTime.SpecifyKind(slot.date, DateTimeKind.Utc)
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
            var centerId = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            var journal = await context.Journals
                .Include(j => j.Group)
                .Include(j => j.Entries)
                .Where(j => j.GroupId == groupId && j.WeekNumber == weekNumber && !j.IsDeleted)
                .FirstOrDefaultAsync();
            if (journal != null && centerId != null)
            {
                if (journal.Group == null)
                {
                    journal.Group = await context.Groups.Include(g => g.Course).FirstOrDefaultAsync(g => g.Id == journal.GroupId);
                }
                if (journal.Group?.Course?.CenterId != centerId)
                {
                    var hasAssignmentAccess = await HasGroupAccessAsync(journal.GroupId);
                    if (!hasAssignmentAccess)
                        return new Response<GetJournalDto>(HttpStatusCode.Forbidden, "Дастрасӣ манъ аст");
                }
            }

            if (journal == null)
                return new Response<GetJournalDto>(HttpStatusCode.NotFound, "Журнал ёфт нашуд");

            var studentIds = journal.Entries.Select(e => e.StudentId).Distinct().ToList();
            var students = await context.Students
                .Where(s => studentIds.Contains(s.Id) && !s.IsDeleted)
                .Select(s => new { s.Id, s.FullName, IsActive = s.ActiveStatus == ActiveStatus.Active })
                .ToListAsync();

            var totalsByStudent = journal.Entries
                .Where(e => !e.IsDeleted)
                .GroupBy(e => e.StudentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Where(x => x.Grade.HasValue).Sum(x => x.Grade!.Value)
                         + g.Where(x => x.BonusPoints.HasValue).Sum(x => x.BonusPoints!.Value)
                );

            var progresses = students
                .Select(s => new { s, total = totalsByStudent.TryGetValue(s.Id, out var t) ? t : 0m })
                .OrderByDescending(x => x.total)
                .ThenByDescending(x => x.s.IsActive)
                .ThenBy(x => x.s.FullName)
                .Select(x => new StudentProgress
                {
                    StudentId = x.s.Id,
                    StudentName = $"{x.s.FullName}".Trim(),
                    WeeklyTotalScores = (double)x.total,
                    StudentEntries = journal.Entries
                        .Where(e => e.StudentId == x.s.Id)
                        .OrderBy(e => e.LessonNumber)
                        .ThenBy(e => e.DayOfWeek)
                        .Select(e => new GetJournalEntryDto
                        {
                            Id = e.Id,
                            DayOfWeek = e.DayOfWeek,
                            DayName = GetDayNameInTajik(e.DayOfWeek),
                            DayShortName = GetDayShortNameInTajik(e.DayOfWeek),
                            LessonNumber = e.LessonNumber,
                            LessonType = e.LessonType,
                            Grade = e.Grade ?? 0,
                            BonusPoints = e.BonusPoints ?? 0,
                            AttendanceStatus = e.AttendanceStatus,
                            Comment = e.Comment,
                            CommentCategory = e.CommentCategory ?? CommentCategory.General,
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

    public async Task<Response<GetJournalDto>> GetLatestJournalAsync(int groupId)
    {
        try
        {
            var centerId2 = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            var journal = await context.Journals
                .Include(j => j.Group)
                .Include(j => j.Entries)
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .OrderByDescending(j => j.WeekNumber)
                .FirstOrDefaultAsync();

            if (journal != null && centerId2 != null)
            {
                if (journal.Group == null)
                {
                    journal.Group = await context.Groups.Include(g => g.Course).FirstOrDefaultAsync(g => g.Id == journal.GroupId);
                }
                if (journal.Group?.Course?.CenterId != centerId2)
                {
                    var hasAssignmentAccess = await HasGroupAccessAsync(journal.GroupId);
                    if (!hasAssignmentAccess)
                        return new Response<GetJournalDto>(HttpStatusCode.Forbidden, "Дастрасӣ манъ аст");
                }
            }

            if (journal == null)
                return new Response<GetJournalDto>(HttpStatusCode.NotFound, "Журналҳо ёфт нашуданд");

            var studentIds = journal.Entries.Select(e => e.StudentId).Distinct().ToList();
            var students = await context.Students
                .Where(s => studentIds.Contains(s.Id) && !s.IsDeleted)
                .Select(s => new { s.Id, s.FullName, IsActive = s.ActiveStatus == ActiveStatus.Active })
                .ToListAsync();

            var totalsByStudent = journal.Entries
                .Where(e => !e.IsDeleted)
                .GroupBy(e => e.StudentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Where(x => x.Grade.HasValue).Sum(x => x.Grade!.Value)
                         + g.Where(x => x.BonusPoints.HasValue).Sum(x => x.BonusPoints!.Value)
                );

            var progresses = students
                .Select(s => new { s, total = totalsByStudent.TryGetValue(s.Id, out var t) ? t : 0m })
                .OrderByDescending(x => x.total)
                .ThenByDescending(x => x.s.IsActive)
                .ThenBy(x => x.s.FullName)
                .Select(x => new StudentProgress
                {
                    StudentId = x.s.Id,
                    StudentName = $"{x.s.FullName}".Trim(),
                    WeeklyTotalScores = (double)x.total,
                    StudentEntries = journal.Entries
                        .Where(e => e.StudentId == x.s.Id)
                        .OrderBy(e => e.LessonNumber)
                        .ThenBy(e => e.DayOfWeek)
                        .Select(e => new GetJournalEntryDto
                        {
                            Id = e.Id,
                            DayOfWeek = e.DayOfWeek,
                            DayName = string.Empty,
                            DayShortName = string.Empty,
                            LessonNumber = e.LessonNumber,
                            LessonType = e.LessonType,
                            Grade = e.Grade ?? 0,
                            BonusPoints = e.BonusPoints ?? 0,
                            AttendanceStatus = e.AttendanceStatus,
                            Comment = e.Comment,
                            CommentCategory = e.CommentCategory ?? CommentCategory.General,
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

    public async Task<Response<GetJournalDto>> GetJournalByDateAsync(int groupId, DateTime dateLocal)
    {
        try
        {
            var centerId3 = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            var groupQuery = context.Groups.Include(g => g.Course).Where(g => !g.IsDeleted).AsQueryable();
            if (centerId3 != null)
            {
                groupQuery = groupQuery.Where(g => g.Course!.CenterId == centerId3);
            }
            var group = await groupQuery.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
                return new Response<GetJournalDto>(HttpStatusCode.NotFound, "Гурӯҳ ёфт нашуд");
            var localDate = DateTime.SpecifyKind(dateLocal.Date, DateTimeKind.Unspecified);
            var localStart = new DateTimeOffset(localDate, TimeSpan.Zero);
            var localEnd = localStart.AddDays(1);
            var journal = await context.Journals
                .Include(j => j.Group)
                .Include(j => j.Entries)
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .FirstOrDefaultAsync(j => localStart <= j.WeekEndDate && localEnd > j.WeekStartDate);
            if (journal != null && centerId3 != null)
            {
                if (journal.Group == null)
                {
                    journal.Group = await context.Groups.Include(g => g.Course).FirstOrDefaultAsync(g => g.Id == journal.GroupId);
                }
                if (journal.Group?.Course?.CenterId != centerId3)
                {
                    var hasAssignmentAccess = await HasGroupAccessAsync(journal.GroupId);
                    if (!hasAssignmentAccess)
                        return new Response<GetJournalDto>(HttpStatusCode.Forbidden, "Дастрасӣ манъ аст");
                }
            }

            if (journal == null)
                return new Response<GetJournalDto>(HttpStatusCode.NotFound, "Журнал барои ин сана ёфт нашуд");

            var studentIds = journal.Entries.Select(e => e.StudentId).Distinct().ToList();
            var students = await context.Students
                .Where(s => studentIds.Contains(s.Id) && !s.IsDeleted)
                .Select(s => new { s.Id, s.FullName, IsActive = s.ActiveStatus == ActiveStatus.Active })
                .ToListAsync();

            var totalsByStudent = journal.Entries
                .Where(e => !e.IsDeleted)
                .GroupBy(e => e.StudentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Where(x => x.Grade.HasValue).Sum(x => x.Grade!.Value)
                         + g.Where(x => x.BonusPoints.HasValue).Sum(x => x.BonusPoints!.Value)
                );

            var progresses = students
                .Select(s => new { s, total = totalsByStudent.TryGetValue(s.Id, out var t) ? t : 0m })
                .OrderByDescending(x => x.total)
                .ThenByDescending(x => x.s.IsActive)
                .ThenBy(x => x.s.FullName)
                .Select(x => new StudentProgress
                {
                    StudentId = x.s.Id,
                    StudentName = $"{x.s.FullName}".Trim(),
                    WeeklyTotalScores = (double)x.total,
                    StudentEntries = journal.Entries
                        .Where(e => e.StudentId == x.s.Id)
                        .OrderBy(e => e.LessonNumber)
                        .ThenBy(e => e.DayOfWeek)
                        .Select(e => new GetJournalEntryDto
                        {
                            Id = e.Id,
                            DayOfWeek = e.DayOfWeek,
                            DayName = string.Empty,
                            DayShortName = string.Empty,
                            LessonNumber = e.LessonNumber,
                            LessonType = e.LessonType,
                            Grade = e.Grade ?? 0,
                            BonusPoints = e.BonusPoints ?? 0,
                            AttendanceStatus = e.AttendanceStatus,
                            Comment = e.Comment,
                            CommentCategory = e.CommentCategory ?? CommentCategory.General,
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
            var entry = await context.JournalEntries
                .Include(e => e.Journal)
                .ThenInclude(j => j.Group)
                .ThenInclude(g => g.Course)
                .FirstOrDefaultAsync(e => e.Id == entryId && !e.IsDeleted);
            if (entry == null)
                return new Response<string>(HttpStatusCode.NotFound, "Элемент ёфт нашуд");

            var centerId4 = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            if (centerId4 != null)
            {
                if (entry.Journal?.Group?.Course?.CenterId != centerId4)
                {
                    var hasAssignmentAccess = await HasGroupAccessAsync(entry.Journal!.GroupId);
                    if (!hasAssignmentAccess)
                        return new Response<string>(HttpStatusCode.Forbidden, "Дастрасӣ манъ аст");
                }
            }

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

    public async Task<Response<string>> BackfillCurrentWeekForStudentAsync(int groupId, int studentId)
    {
        try
        {
            var centerId5 = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            var groupAllowed = await context.Groups.Include(g => g.Course)
                .AnyAsync(g => g.Id == groupId && !g.IsDeleted && (centerId5 == null || g.Course!.CenterId == centerId5));
            if (!groupAllowed)
            {
                var hasAssignmentAccess = await HasGroupAccessAsync(groupId);
                if (!hasAssignmentAccess)
                    return new Response<string>(HttpStatusCode.Forbidden, "Дастрасӣ манъ аст");
            }

            var isActiveMember = await context.StudentGroups
                .AnyAsync(sg => sg.GroupId == groupId && sg.StudentId == studentId && sg.IsActive && !sg.IsDeleted);
            if (!isActiveMember)
            {
                return new Response<string>(HttpStatusCode.OK, "Студент неактивен в группе — backfill пропущен");
            }

            var nowUtc = DateTimeOffset.UtcNow;
            var journal = await context.Journals
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .FirstOrDefaultAsync(j => j.WeekStartDate <= nowUtc && nowUtc <= j.WeekEndDate);

            if (journal == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, "Журнали ҳафтаи ҷорӣ барои ин гурӯҳ ёфт нашуд");
            }

            var slots = await context.JournalEntries
                .Where(e => e.JournalId == journal.Id && !e.IsDeleted)
                .Select(e => new
                {
                    e.DayOfWeek,
                    e.LessonNumber,
                    e.LessonType,
                    e.StartTime,
                    e.EndTime,
                    e.EntryDate
                })
                .Distinct()
                .ToListAsync();

            if (slots.Count == 0)
            {
                return new Response<string>(HttpStatusCode.BadRequest, "Барои ин ҳафта ягон слот вуҷуд надорад");
            }

            int created = 0;
            foreach (var s in slots)
            {
                var exists = await context.JournalEntries.AnyAsync(e =>
                    e.JournalId == journal.Id &&
                    e.StudentId == studentId &&
                    e.DayOfWeek == s.DayOfWeek &&
                    e.LessonNumber == s.LessonNumber &&
                    !e.IsDeleted);

                if (exists) continue;

                var entry = new JournalEntry
                {
                    JournalId = journal.Id,
                    StudentId = studentId,
                    DayOfWeek = s.DayOfWeek,
                    LessonNumber = s.LessonNumber,
                    LessonType = s.LessonType,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    AttendanceStatus = AttendanceStatus.Absent,
                    EntryDate = DateTime.SpecifyKind(s.EntryDate, DateTimeKind.Utc)
                };
                await context.JournalEntries.AddAsync(entry);
                created++;
            }

            if (created == 0)
                return new Response<string>(HttpStatusCode.OK, "Барои донишҷӯ ягон entry-и нав лозим нашуд");

            await context.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.Created, $"Создадено {created} запис(ов) для студента");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<string>> BackfillCurrentWeekForStudentsAsync(int groupId, IEnumerable<int> studentIds)
    {
        try
        {
            var ids = studentIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Response<string>(HttpStatusCode.BadRequest, "Студенты не указаны");

            var centerId6 = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            var allowed = await context.Groups.Include(g => g.Course)
                .AnyAsync(g => g.Id == groupId && !g.IsDeleted && (centerId6 == null || g.Course!.CenterId == centerId6));
            if (!allowed)
            {
                var hasAssignmentAccess = await HasGroupAccessAsync(groupId);
                if (!hasAssignmentAccess)
                    return new Response<string>(HttpStatusCode.Forbidden, "Дастрасӣ манъ аст");
            }

            var activeIds = await context.StudentGroups
                .Where(sg => sg.GroupId == groupId && ids.Contains(sg.StudentId) && sg.IsActive && !sg.IsDeleted)
                .Select(sg => sg.StudentId)
                .Distinct()
                .ToListAsync();
            if (activeIds.Count == 0)
            {
                return new Response<string>(HttpStatusCode.OK, "Нет активных студентов для backfill");
            }

            var nowUtc = DateTimeOffset.UtcNow;
            var journal = await context.Journals
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .FirstOrDefaultAsync(j => j.WeekStartDate <= nowUtc && nowUtc <= j.WeekEndDate);

            if (journal == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, "Журнали ҳафтаи ҷорӣ барои ин гурӯҳ ёфт нашуд");
            }

            var slots = await context.JournalEntries
                .Where(e => e.JournalId == journal.Id && !e.IsDeleted)
                .Select(e => new
                {
                    e.DayOfWeek,
                    e.LessonNumber,
                    e.LessonType,
                    e.StartTime,
                    e.EndTime,
                    e.EntryDate
                })
                .Distinct()
                .ToListAsync();

            if (slots.Count == 0)
            {
                return new Response<string>(HttpStatusCode.BadRequest, "Барои ин ҳафта ягон слот вуҷуд надорад");
            }

            int createdTotal = 0;
            foreach (var studentId in activeIds)
            {
                foreach (var s in slots)
                {
                    var exists = await context.JournalEntries.AnyAsync(e =>
                        e.JournalId == journal.Id &&
                        e.StudentId == studentId &&
                        e.DayOfWeek == s.DayOfWeek &&
                        e.LessonNumber == s.LessonNumber &&
                        !e.IsDeleted);

                    if (exists) continue;

                    var entry = new JournalEntry
                    {
                        JournalId = journal.Id,
                        StudentId = studentId,
                        DayOfWeek = s.DayOfWeek,
                        LessonNumber = s.LessonNumber,
                        LessonType = s.LessonType,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        AttendanceStatus = AttendanceStatus.Absent,
                        EntryDate = DateTime.SpecifyKind(s.EntryDate, DateTimeKind.Utc)
                    };
                    await context.JournalEntries.AddAsync(entry);
                    createdTotal++;
                }
            }

            if (createdTotal == 0)
                return new Response<string>(HttpStatusCode.OK, "Ягон сабти нав лозим нашуд");

            await context.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.Created, $"Создадено {createdTotal} запис(ов) для студентов");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<string>> RemoveFutureEntriesForStudentAsync(int groupId, int studentId)
    {
        try
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var centerId7 = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            var allowed2 = await context.Groups.Include(g => g.Course)
                .AnyAsync(g => g.Id == groupId && !g.IsDeleted && (centerId7 == null || g.Course!.CenterId == centerId7));
            if (!allowed2)
            {
                var hasAssignmentAccess = await HasGroupAccessAsync(groupId);
                if (!hasAssignmentAccess)
                    return new Response<string>(HttpStatusCode.Forbidden, "Дастрасӣ манъ аст");
            }

            var futureJournalIds = await context.Journals
                .Where(j => j.GroupId == groupId && !j.IsDeleted && j.WeekStartDate > nowUtc)
                .Select(j => j.Id)
                .ToListAsync();

            if (futureJournalIds.Count == 0)
                return new Response<string>(HttpStatusCode.OK, "Будущих недель нет — удалять нечего");

            var futureEntries = await context.JournalEntries
                .Where(e => futureJournalIds.Contains(e.JournalId) && e.StudentId == studentId && !e.IsDeleted)
                .ToListAsync();

            if (futureEntries.Count == 0)
                return new Response<string>(HttpStatusCode.OK, "Для студента будущих записей нет");

            foreach (var e in futureEntries)
            {
                e.IsDeleted = true;
                e.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await context.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, $"Удалено будущих записей: {futureEntries.Count}");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private static List<int> ParseLessonDays(string? lessonDays)
    {
        if (string.IsNullOrWhiteSpace(lessonDays)) return new List<int>();
        return lessonDays.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim())
            .Where(d => int.TryParse(d, out var v) && v >= 1 && v <= 7) // Changed from 0-6 to 1-7
            .Select(int.Parse)
            .Distinct()
            .ToList();
    }
    
    private static int ConvertCrmToDotNetDayOfWeek(int crmDayOfWeek)
    {
        return crmDayOfWeek switch
        {
            1 => 1, // Душанбе
            2 => 2, // Сешанбе
            3 => 3, // Чоршанбе
            4 => 4, // Панҷшанбе
            5 => 5, // Ҷумъа
            6 => 6, // Шанбе
            7 => 0, // Якшанбе (DotNet Sunday)
            _ => throw new ArgumentOutOfRangeException(nameof(crmDayOfWeek), "Рӯзи ҳафта бояд аз 1 то 7 бошад")
        };
    }

    private static int ConvertDotNetToCrmDayOfWeek(int dotNetDayOfWeek)
    {
        return dotNetDayOfWeek switch
        {
            0 => 7, // Якшанбе (DotNet Sunday) -> CRM Sunday
            1 => 1, // Душанбе (DotNet Monday) -> CRM Monday
            2 => 2, // Сешанбе (DotNet Tuesday) -> CRM Tuesday
            3 => 3, // Чоршанбе (DotNet Wednesday) -> CRM Wednesday
            4 => 4, // Панҷшанбе (DotNet Thursday) -> CRM Thursday
            5 => 5, // Ҷумъа (DotNet Friday) -> CRM Friday
            6 => 6, // Шанбе (DotNet Saturday) -> CRM Saturday
            _ => throw new ArgumentOutOfRangeException(nameof(dotNetDayOfWeek), "Рӯзи ҳафта бояд аз 0 то 6 бошад")
        };
    }

    private static string GetDayNameInTajik(int crmDayOfWeek)
    {
        return crmDayOfWeek switch
        {
            1 => "Душанбе",
            2 => "Сешанбе", 
            3 => "Чоршанбе",
            4 => "Панҷшанбе",
            5 => "Ҷумъа",
            6 => "Шанбе",
            7 => "Якшанбе",
            _ => "Номаълум"
        };
    }

    private static string GetDayShortNameInTajik(int crmDayOfWeek)
    {
        return crmDayOfWeek switch
        {
            1 => "Ду",
            2 => "Се", 
            3 => "Чо",
            4 => "Па",
            5 => "Ҷу",
            6 => "Ша",
            7 => "Як",
            _ => "Н"
        };
    }
    
    private static LessonType DetermineLessonType(bool hasWeeklyExam, int weekNumber, int lessonNumber, int totalLessons)
    {
        if (hasWeeklyExam)
        {
            
            return lessonNumber == totalLessons ? LessonType.Exam : LessonType.Regular;
        }
        else
        {
            var isFourthWeekEnd = weekNumber % 4 == 0; //4, 8, 12, 16, ...
            var isLastLessonOfWeek = lessonNumber == totalLessons;
            return (isFourthWeekEnd && isLastLessonOfWeek) ? LessonType.Exam : LessonType.Regular;
        }
    }

    public async Task<Response<List<StudentWeekTotalsDto>>> GetStudentWeekTotalsAsync(int groupId, int weekNumber)
    {
        try
        {
            var journal = await context.Journals
                .Include(j => j.Entries)
                .Include(j => j.Group)
                .FirstOrDefaultAsync(j => j.GroupId == groupId && j.WeekNumber == weekNumber && !j.IsDeleted);

            if (journal == null)
                return new Response<List<StudentWeekTotalsDto>>(HttpStatusCode.NotFound, "Журнал ёфт нашуд");

            var entries = journal.Entries.Where(e => !e.IsDeleted).ToList();

            var studentIds = entries.Select(e => e.StudentId).Distinct().ToList();
            var students = await context.Students
                .Where(s => studentIds.Contains(s.Id) && !s.IsDeleted)
                .Select(s => new { s.Id, s.FullName })
                .ToListAsync();

            var totals = students.Select(s => new StudentWeekTotalsDto
            {
                StudentId = s.Id,
                StudentName = s.FullName,
                TotalPoints =
                    entries.Where(e => e.StudentId == s.Id && e.Grade.HasValue).Select(e => e.Grade!.Value).Sum() +
                    entries.Where(e => e.StudentId == s.Id && e.BonusPoints.HasValue).Select(e => e.BonusPoints!.Value)
                        .Sum()
            }).OrderBy(t => t.StudentName).ToList();

            return new Response<List<StudentWeekTotalsDto>>(totals);
        }
        catch (Exception ex)
        {
            return new Response<List<StudentWeekTotalsDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<GroupWeeklyTotalsDto>> GetGroupWeeklyTotalsAsync(int groupId, int? weekId = null)
    {
        try
        {
            var journalsQuery = context.Journals
                .Include(j => j.Entries)
                .Include(j => j.Group)
                .Where(j => j.GroupId == groupId && !j.IsDeleted);
            
            if (weekId.HasValue)
            {
                journalsQuery = journalsQuery.Where(j => j.WeekNumber == weekId.Value);
            }

            var journals = await journalsQuery
                .OrderBy(j => j.WeekNumber)
                .ToListAsync();

            if (journals.Count == 0)
                return new Response<GroupWeeklyTotalsDto>(HttpStatusCode.NotFound, "Журналҳо ёфт нашуданд");

            var allEntries = journals.SelectMany(j => j.Entries).Where(e => !e.IsDeleted).ToList();
            var studentIds = allEntries.Select(e => e.StudentId).Distinct().ToList();
            var students = await context.Students
                .Where(s => studentIds.Contains(s.Id) && !s.IsDeleted)
                .Select(s => new { s.Id, s.FullName, IsActive = s.ActiveStatus == ActiveStatus.Active })
                .ToListAsync();
            
            var result = new GroupWeeklyTotalsDto
            {
                GroupId = groupId,
                GroupName = journals.First().Group?.Name ?? string.Empty
            };

            foreach (var journal in journals)
            {
                var weekEntries = journal.Entries.Where(e => !e.IsDeleted).ToList();
                var week = new WeekTotalsDto
                {
                    WeekNumber = journal.WeekNumber,
                    WeekStartDate = journal.WeekStartDate,
                    WeekEndDate = journal.WeekEndDate
                };

                week.Students = students
                    .Select(s =>
                    {
                        var hasEntries = weekEntries.Any(e => e.StudentId == s.Id);
                        var total = hasEntries
                            ? weekEntries.Where(e => e.StudentId == s.Id && e.Grade.HasValue).Sum(e => e.Grade!.Value)
                              + weekEntries.Where(e => e.StudentId == s.Id && e.BonusPoints.HasValue).Sum(e => e.BonusPoints!.Value)
                            : 0m;
                        return new StudentWeekPointsDto
                        {
                            StudentId = s.Id,
                            StudentName = s.FullName,
                            IsActive = hasEntries, // present in this week
                            TotalPoints = total
                        };
                    })
                    .OrderByDescending(x => x.TotalPoints)
                    .ThenByDescending(x => x.IsActive)
                    .ThenBy(x => x.StudentName)
                    .ToList();

                result.Weeks.Add(week);
            }

            if (!weekId.HasValue)
            {
                result.StudentAggregates = students
                    .Select(s =>
                    {
                        var totals = result.Weeks
                            .SelectMany(w => w.Students)
                            .Where(x => x.StudentId == s.Id && x.IsActive) // average over active weeks only
                            .Select(x => x.TotalPoints)
                            .ToList();
                        var sum = totals.Sum();
                        var avg = totals.Count > 0 ? Math.Round((double)totals.Average(), 2) : 0d;
                        return new StudentAggregateDto
                        {
                            StudentId = s.Id,
                            StudentName = s.FullName,
                            TotalPointsAllWeeks = sum,
                            AveragePointsPerWeek = avg,
                            IsActive = s.IsActive
                        };
                    })
                    .OrderByDescending(a => a.TotalPointsAllWeeks)
                    .ThenByDescending(a => a.IsActive)
                    .ThenBy(a => a.StudentName)
                    .ToList();
            }

            return new Response<GroupWeeklyTotalsDto>(result);
        }
        catch (Exception ex)
        {
            return new Response<GroupWeeklyTotalsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<GroupPassStatsDto>> GetGroupPassStatsAsync(int groupId, decimal threshold)
    {
        try
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null)
                return new Response<GroupPassStatsDto>(HttpStatusCode.NotFound, "Гурӯҳ ёфт нашуд");
            var totalsResponse = await GetGroupWeeklyTotalsAsync(groupId, null);
            if (totalsResponse.StatusCode == (int)HttpStatusCode.NotFound)
            {
                return new Response<GroupPassStatsDto>(new GroupPassStatsDto
                {
                    GroupId = groupId,
                    GroupName = group.Name,
                    TotalStudents = 0,
                    PassedCount = 0,
                    Threshold = threshold
                });
            }

            var aggregates = totalsResponse.Data.StudentAggregates;
            var totalStudents = aggregates.Count;
            var passedCount = aggregates.Count(a => (decimal)a.AveragePointsPerWeek >= threshold);

            var dto = new GroupPassStatsDto
            {
                GroupId = groupId,
                GroupName = totalsResponse.Data.GroupName,
                TotalStudents = totalStudents,
                PassedCount = passedCount,
                Threshold = threshold
            };

            return new Response<GroupPassStatsDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GroupPassStatsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<List<int>>> GetGroupWeekNumbersAsync(int groupId)
    {
        try
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null)
                return new Response<List<int>>(HttpStatusCode.NotFound, "Гурӯҳ ёфт нашуд");

            var existingWeeks = await context.Journals
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .Select(j => j.WeekNumber)
                .OrderBy(w => w)
                .ToListAsync();

            if (existingWeeks.Count == 0)
            {
                return new Response<List<int>>(new List<int>());
            }

            var maxWeek = existingWeeks.Max();
            
            var weekNumbers = Enumerable.Range(1, maxWeek).ToList();
            
            return new Response<List<int>>(weekNumbers);
        }
        catch (Exception ex)
        {
            return new Response<List<int>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private async Task<bool> HasGroupAccessAsync(int groupId)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role || string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roles.Contains("SuperAdmin") || roles.Contains("Admin") || roles.Contains("Manager"))
            return true;

        var principalType = user.FindFirst("PrincipalType")?.Value;
        var idStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? user.FindFirst("nameid")?.Value;
        if (!int.TryParse(idStr, out var principalId) || principalId <= 0)
            return false;

        if (roles.Contains("Mentor") || string.Equals(principalType, "Mentor", StringComparison.OrdinalIgnoreCase))
        {
            var isPrimaryMentor = await context.Groups
                .AnyAsync(g => g.Id == groupId && !g.IsDeleted && g.MentorId == principalId);
            if (isPrimaryMentor) return true;

            var isCoMentor = await context.MentorGroups
                .AnyAsync(mg => mg.GroupId == groupId && mg.MentorId == principalId && (mg.IsActive ?? true) && !mg.IsDeleted);
            return isCoMentor;
        }

        if (roles.Contains("Student") || string.Equals(principalType, "Student", StringComparison.OrdinalIgnoreCase))
        {
            var isMember = await context.StudentGroups
                .AnyAsync(sg => sg.GroupId == groupId && sg.StudentId == principalId && sg.IsActive && !sg.IsDeleted);
            return isMember;
        }

        return false;
    }
}
