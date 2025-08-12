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
                lessonDays = new List<int> { 1, 2, 3, 4, 5 };
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
                var dowZero = (int)cursor.DayOfWeek;
                if (lessonDays.Contains(dowZero))
                {
                    var dayOfWeekOneBased = dowZero % 7 + 1;
                    plannedSlots.Add((cursor, dayOfWeekOneBased, plannedSlots.Count + 1));
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
                var isLast = slot.lessonNumber == targetLessons;
                var lessonType = group.HasWeeklyExam && isLast ? LessonType.Exam : LessonType.Regular;
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
            var journal = await context.Journals
                .Include(j => j.Group)
                .Include(j => j.Entries)
                .FirstOrDefaultAsync(j => j.GroupId == groupId && j.WeekNumber == weekNumber && !j.IsDeleted);

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
                StudentEntries = journal.Entries
                    .Where(e => e.StudentId == x.s.Id)
                    .OrderBy(e => e.LessonNumber)
                    .ThenBy(e => e.DayOfWeek)
                    .Select(e => new GetJournalEntryDto
                    {
                        Id = e.Id,
                        DayOfWeek = e.DayOfWeek,
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
            var journal = await context.Journals
                .Include(j => j.Group)
                .Include(j => j.Entries)
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .OrderByDescending(j => j.WeekNumber)
                .FirstOrDefaultAsync();

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
                StudentEntries = journal.Entries
                    .Where(e => e.StudentId == x.s.Id)
                    .OrderBy(e => e.LessonNumber)
                    .ThenBy(e => e.DayOfWeek)
                    .Select(e => new GetJournalEntryDto
                    {
                        Id = e.Id,
                        DayOfWeek = e.DayOfWeek,
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
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null)
                return new Response<GetJournalDto>(HttpStatusCode.NotFound, "Гурӯҳ ёфт нашуд");

            // Преобразуем локальную дату в UTC-сутки для сопоставления с EntryDate (UTC)
            var localDate = DateTime.SpecifyKind(dateLocal.Date, DateTimeKind.Unspecified);
            var localStart = new DateTimeOffset(localDate, TimeSpan.Zero);
            var localEnd = localStart.AddDays(1);

            // Находим журнал, у которого интервал [WeekStartDate, WeekEndDate] покрывает указанную дату
            var journal = await context.Journals
                .Include(j => j.Group)
                .Include(j => j.Entries)
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .FirstOrDefaultAsync(j => localStart <= j.WeekEndDate && localEnd > j.WeekStartDate);

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
                StudentEntries = journal.Entries
                    .Where(e => e.StudentId == x.s.Id)
                    .OrderBy(e => e.LessonNumber)
                    .ThenBy(e => e.DayOfWeek)
                    .Select(e => new GetJournalEntryDto
                    {
                        Id = e.Id,
                        DayOfWeek = e.DayOfWeek,
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

    public async Task<Response<string>> BackfillCurrentWeekForStudentAsync(int groupId, int studentId)
    {
        try
        {
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
            var ids = studentIds?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0)
                return new Response<string>(HttpStatusCode.BadRequest, "Студенты не указаны");

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
                    entries.Where(e => e.StudentId == s.Id && e.BonusPoints.HasValue).Select(e => e.BonusPoints!.Value).Sum()
            }).OrderBy(t => t.StudentName).ToList();

            return new Response<List<StudentWeekTotalsDto>>(totals);
        }
        catch (Exception ex)
        {
            return new Response<List<StudentWeekTotalsDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    
    public async Task<Response<GroupWeeklyTotalsDto>> GetGroupWeeklyTotalsAsync(int groupId)
    {
        try
        {
            var journals = await context.Journals
                .Include(j => j.Entries)
                .Include(j => j.Group)
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
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
            var studentIsActive = students.ToDictionary(s => s.Id, s => s.IsActive);

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
                    .Select(s => new StudentWeekPointsDto
                    {
                        StudentId = s.Id,
                        StudentName = s.FullName,
                        IsActive = s.IsActive,
                        TotalPoints =
                            weekEntries.Where(e => e.StudentId == s.Id && e.Grade.HasValue).Sum(e => e.Grade!.Value) +
                            weekEntries.Where(e => e.StudentId == s.Id && e.BonusPoints.HasValue).Sum(e => e.BonusPoints!.Value)
                    })
                    .OrderByDescending(x => x.TotalPoints)
                    .ThenByDescending(x => x.IsActive)
                    .ThenBy(x => x.StudentName)
                    .ToList();

                result.Weeks.Add(week);
            }

            result.StudentAggregates = students
                .Select(s =>
                {
                    var totals = result.Weeks
                        .SelectMany(w => w.Students)
                        .Where(x => x.StudentId == s.Id)
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

            return new Response<GroupWeeklyTotalsDto>(result);
        }
        catch (Exception ex)
        {
            return new Response<GroupWeeklyTotalsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
