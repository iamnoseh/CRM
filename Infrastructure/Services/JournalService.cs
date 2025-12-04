using System.Net;
using Domain.DTOs.Journal;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Constants;
using Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Services;

public class JournalService(DataContext context, IHttpContextAccessor httpContextAccessor) : IJournalService
{
    #region GenerateWeeklyJournalAsync

    public async Task<Response<string>> GenerateWeeklyJournalAsync(int groupId, int weekNumber)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var group = await GetGroupWithCenterFilterAsync(groupId, centerId);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Journal.GroupNotFound);

            var existingWeeks = await context.Journals
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .Select(j => j.WeekNumber)
                .ToListAsync();
            var maxExistingWeek = existingWeeks.Count == 0 ? 0 : existingWeeks.Max();
            var expectedNextWeek = maxExistingWeek + 1;

            if (weekNumber > group.TotalWeeks)
                return new Response<string>(HttpStatusCode.BadRequest,
                    string.Format(Messages.Journal.WeekExceedsTotal, weekNumber, group.TotalWeeks, expectedNextWeek));

            if (weekNumber <= maxExistingWeek)
                return new Response<string>(HttpStatusCode.BadRequest,
                    string.Format(Messages.Journal.WeekSequenceError, expectedNextWeek));

            if (weekNumber != expectedNextWeek)
                return new Response<string>(HttpStatusCode.BadRequest,
                    string.Format(Messages.Journal.WeekSequenceError, expectedNextWeek));

            var existing = await context.Journals
                .FirstOrDefaultAsync(j => j.GroupId == groupId && j.WeekNumber == weekNumber && !j.IsDeleted);
            if (existing != null)
                return new Response<string>(HttpStatusCode.OK, Messages.Journal.WeekAlreadyExists);

            var lessonDays = ParseLessonDays(group.LessonDays);
            if (lessonDays.Count == 0)
                lessonDays = new List<int> { 2, 3, 4, 5, 6 };

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

            var plannedSlots = GeneratePlannedSlots(cursor, lessonDays, targetLessons);
            var journal = await CreateJournalWithEntriesAsync(group, weekNumber, plannedSlots, targetLessons);

            return new Response<string>(HttpStatusCode.Created, Messages.Journal.WeekCreated);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GenerateWeeklyJournalFromCustomDateAsync

    public async Task<Response<string>> GenerateWeeklyJournalFromCustomDateAsync(int groupId, DateTime startDate)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var group = await GetGroupWithCenterFilterAsync(groupId, centerId);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Journal.GroupNotFound);

            var existingWeek1 = await context.Journals
                .FirstOrDefaultAsync(j => j.GroupId == groupId && j.WeekNumber == 1 && !j.IsDeleted);
            if (existingWeek1 != null)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Journal.Week1AlreadyExists);

            var lessonDays = ParseLessonDays(group.LessonDays);
            if (lessonDays.Count == 0)
                lessonDays = new List<int> { 2, 3, 4, 5, 6 };

            var targetLessons = 6;
            var plannedSlots = GeneratePlannedSlots(startDate.Date, lessonDays, targetLessons);
            await CreateJournalWithEntriesAsync(group, 1, plannedSlots, targetLessons);

            return new Response<string>(HttpStatusCode.Created,
                string.Format(Messages.Journal.WeekCreatedFromDate, startDate.ToString("dd.MM.yyyy")));
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetJournalAsync

    public async Task<Response<GetJournalDto>> GetJournalAsync(int groupId, int weekNumber)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var journal = await context.Journals
                .Include(j => j.Group)
                .Include(j => j.Entries)
                .Where(j => j.GroupId == groupId && j.WeekNumber == weekNumber && !j.IsDeleted)
                .FirstOrDefaultAsync();

            if (journal != null && centerId != null)
            {
                if (!await HasJournalAccessAsync(journal, centerId.Value))
                    return new Response<GetJournalDto>(HttpStatusCode.Forbidden, Messages.Journal.AccessDenied);
            }

            if (journal == null)
                return new Response<GetJournalDto>(HttpStatusCode.NotFound, Messages.Journal.NotFound);

            var dto = await MapToJournalDtoAsync(journal);
            return new Response<GetJournalDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetJournalDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetLatestJournalAsync

    public async Task<Response<GetJournalDto>> GetLatestJournalAsync(int groupId)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var journal = await context.Journals
                .Include(j => j.Group)
                .Include(j => j.Entries)
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .OrderByDescending(j => j.WeekNumber)
                .FirstOrDefaultAsync();

            if (journal != null && centerId != null)
            {
                if (!await HasJournalAccessAsync(journal, centerId.Value))
                    return new Response<GetJournalDto>(HttpStatusCode.Forbidden, Messages.Journal.AccessDenied);
            }

            if (journal == null)
                return new Response<GetJournalDto>(HttpStatusCode.NotFound, Messages.Journal.NoJournalsFound);

            var dto = await MapToJournalDtoAsync(journal, false);
            return new Response<GetJournalDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetJournalDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetJournalByDateAsync

    public async Task<Response<GetJournalDto>> GetJournalByDateAsync(int groupId, DateTime dateLocal)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var group = await GetGroupWithCenterFilterAsync(groupId, centerId);
            if (group == null)
                return new Response<GetJournalDto>(HttpStatusCode.NotFound, Messages.Journal.GroupNotFound);

            var localDate = DateTime.SpecifyKind(dateLocal.Date, DateTimeKind.Unspecified);
            var localStart = new DateTimeOffset(localDate, TimeSpan.Zero);
            var localEnd = localStart.AddDays(1);

            var journal = await context.Journals
                .Include(j => j.Group)
                .Include(j => j.Entries)
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .FirstOrDefaultAsync(j => localStart <= j.WeekEndDate && localEnd > j.WeekStartDate);

            if (journal != null && centerId != null)
            {
                if (!await HasJournalAccessAsync(journal, centerId.Value))
                    return new Response<GetJournalDto>(HttpStatusCode.Forbidden, Messages.Journal.AccessDenied);
            }

            if (journal == null)
                return new Response<GetJournalDto>(HttpStatusCode.NotFound, Messages.Journal.JournalNotFoundForDate);

            var dto = await MapToJournalDtoAsync(journal, false);
            return new Response<GetJournalDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetJournalDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region UpdateEntryAsync

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
                return new Response<string>(HttpStatusCode.NotFound, Messages.Journal.EntryNotFound);

            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId != null)
            {
                var entryCenterId = entry.Journal?.Group?.Course?.CenterId;
                if (entryCenterId.HasValue && entryCenterId.Value != centerId.Value)
                {
                    var groupIdForAccess = entry.Journal?.GroupId;
                    var hasAccess = groupIdForAccess.HasValue && await HasGroupAccessAsync(groupIdForAccess.Value);
                    if (!hasAccess)
                        return new Response<string>(HttpStatusCode.Forbidden, Messages.Journal.AccessDenied);
                }
            }

            if (request.Grade.HasValue) entry.Grade = request.Grade.Value;
            if (request.BonusPoints.HasValue) entry.BonusPoints = request.BonusPoints.Value;
            if (request.AttendanceStatus.HasValue) entry.AttendanceStatus = request.AttendanceStatus.Value;
            if (!string.IsNullOrWhiteSpace(request.Comment))
            {
                entry.Comment = request.Comment;
                var currentUser = httpContextAccessor.HttpContext?.User;
                if (currentUser != null)
                {
                    var userIdClaim = currentUser.FindFirst("UserId")?.Value
                                      ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                      ?? currentUser.FindFirst("nameid")?.Value;
                    var userNameClaim = currentUser.FindFirst("Fullname")?.Value
                                        ?? currentUser.FindFirst(ClaimTypes.Name)?.Value
                                        ?? currentUser.FindFirst("unique_name")?.Value;

                    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                        entry.CommentAuthorId = userId;

                    if (!string.IsNullOrEmpty(userNameClaim))
                        entry.CommentAuthorName = userNameClaim;
                }
            }
            if (request.CommentCategory.HasValue) entry.CommentCategory = request.CommentCategory.Value;

            entry.UpdatedAt = DateTimeOffset.UtcNow;
            context.JournalEntries.Update(entry);
            await context.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, Messages.Journal.Updated);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region BackfillCurrentWeekForStudentAsync

    public async Task<Response<string>> BackfillCurrentWeekForStudentAsync(int groupId, int studentId)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var groupAllowed = await context.Groups.Include(g => g.Course)
                .AnyAsync(g => g.Id == groupId && !g.IsDeleted && (centerId == null || g.Course!.CenterId == centerId));
            if (!groupAllowed && !await HasGroupAccessAsync(groupId))
                return new Response<string>(HttpStatusCode.Forbidden, Messages.Journal.AccessDenied);

            var isActiveMember = await context.StudentGroups
                .AnyAsync(sg => sg.GroupId == groupId && sg.StudentId == studentId && sg.IsActive && !sg.IsDeleted);
            if (!isActiveMember)
                return new Response<string>(HttpStatusCode.OK, Messages.Journal.StudentNotActive);

            var nowUtc = DateTimeOffset.UtcNow;
            var journal = await context.Journals
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .FirstOrDefaultAsync(j => j.WeekStartDate <= nowUtc && nowUtc <= j.WeekEndDate);

            if (journal == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Journal.CurrentWeekNotFound);

            var created = await BackfillStudentEntriesAsync(journal.Id, studentId);
            if (created == 0)
                return new Response<string>(HttpStatusCode.OK, Messages.Journal.NoNewEntriesNeeded);

            await context.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.Created, string.Format(Messages.Journal.EntriesCreated, created));
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region BackfillCurrentWeekForStudentsAsync

    public async Task<Response<string>> BackfillCurrentWeekForStudentsAsync(int groupId, IEnumerable<int> studentIds)
    {
        try
        {
            var ids = studentIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Response<string>(HttpStatusCode.BadRequest, Messages.Journal.NoStudentsSpecified);

            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var allowed = await context.Groups.Include(g => g.Course)
                .AnyAsync(g => g.Id == groupId && !g.IsDeleted && (centerId == null || g.Course!.CenterId == centerId));
            if (!allowed && !await HasGroupAccessAsync(groupId))
                return new Response<string>(HttpStatusCode.Forbidden, Messages.Journal.AccessDenied);

            var activeIds = await context.StudentGroups
                .Where(sg => sg.GroupId == groupId && ids.Contains(sg.StudentId) && sg.IsActive && !sg.IsDeleted)
                .Select(sg => sg.StudentId)
                .Distinct()
                .ToListAsync();
            if (activeIds.Count == 0)
                return new Response<string>(HttpStatusCode.OK, Messages.Journal.NoActiveStudents);

            var nowUtc = DateTimeOffset.UtcNow;
            var journal = await context.Journals
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .FirstOrDefaultAsync(j => j.WeekStartDate <= nowUtc && nowUtc <= j.WeekEndDate);

            if (journal == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Journal.CurrentWeekNotFound);

            int createdTotal = 0;
            foreach (var studentId in activeIds)
            {
                createdTotal += await BackfillStudentEntriesAsync(journal.Id, studentId);
            }

            if (createdTotal == 0)
                return new Response<string>(HttpStatusCode.OK, Messages.Journal.NoNewEntriesNeeded);

            await context.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.Created, string.Format(Messages.Journal.EntriesCreatedMultiple, createdTotal));
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region RemoveFutureEntriesForStudentAsync

    public async Task<Response<string>> RemoveFutureEntriesForStudentAsync(int groupId, int studentId)
    {
        try
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var allowed = await context.Groups.Include(g => g.Course)
                .AnyAsync(g => g.Id == groupId && !g.IsDeleted && (centerId == null || g.Course!.CenterId == centerId));
            if (!allowed && !await HasGroupAccessAsync(groupId))
                return new Response<string>(HttpStatusCode.Forbidden, Messages.Journal.AccessDenied);

            var futureJournalIds = await context.Journals
                .Where(j => j.GroupId == groupId && !j.IsDeleted && j.WeekStartDate > nowUtc)
                .Select(j => j.Id)
                .ToListAsync();

            if (futureJournalIds.Count == 0)
                return new Response<string>(HttpStatusCode.OK, Messages.Journal.NoFutureWeeks);

            var futureEntries = await context.JournalEntries
                .Where(e => futureJournalIds.Contains(e.JournalId) && e.StudentId == studentId && !e.IsDeleted)
                .ToListAsync();

            if (futureEntries.Count == 0)
                return new Response<string>(HttpStatusCode.OK, Messages.Journal.NoFutureEntries);

            foreach (var e in futureEntries)
            {
                e.IsDeleted = true;
                e.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await context.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, string.Format(Messages.Journal.FutureEntriesDeleted, futureEntries.Count));
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetStudentWeekTotalsAsync

    public async Task<Response<List<StudentWeekTotalsDto>>> GetStudentWeekTotalsAsync(int groupId, int weekNumber)
    {
        try
        {
            var journal = await context.Journals
                .Include(j => j.Entries)
                .Include(j => j.Group)
                .FirstOrDefaultAsync(j => j.GroupId == groupId && j.WeekNumber == weekNumber && !j.IsDeleted);

            if (journal == null)
                return new Response<List<StudentWeekTotalsDto>>(HttpStatusCode.NotFound, Messages.Journal.NotFound);

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
                    entries.Where(e => e.StudentId == s.Id && e.Grade.HasValue).Sum(e => e.Grade!.Value) +
                    entries.Where(e => e.StudentId == s.Id && e.BonusPoints.HasValue).Sum(e => e.BonusPoints!.Value)
            }).OrderBy(t => t.StudentName).ToList();

            return new Response<List<StudentWeekTotalsDto>>(totals);
        }
        catch (Exception ex)
        {
            return new Response<List<StudentWeekTotalsDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetGroupWeeklyTotalsAsync

    public async Task<Response<GroupWeeklyTotalsDto>> GetGroupWeeklyTotalsAsync(int groupId, int? weekId = null)
    {
        try
        {
            var journalsQuery = context.Journals
                .Include(j => j.Entries)
                .Include(j => j.Group)
                .Where(j => j.GroupId == groupId && !j.IsDeleted);

            if (weekId.HasValue)
                journalsQuery = journalsQuery.Where(j => j.WeekNumber == weekId.Value);

            var journals = await journalsQuery.OrderBy(j => j.WeekNumber).ToListAsync();

            if (journals.Count == 0)
                return new Response<GroupWeeklyTotalsDto>(HttpStatusCode.NotFound, Messages.Journal.NoJournalsFound);

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
                    WeekEndDate = journal.WeekEndDate,
                    Students = students
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
                                IsActive = hasEntries,
                                TotalPoints = total
                            };
                        })
                        .OrderByDescending(x => x.TotalPoints)
                        .ThenByDescending(x => x.IsActive)
                        .ThenBy(x => x.StudentName)
                        .ToList()
                };
                result.Weeks.Add(week);
            }

            if (!weekId.HasValue)
            {
                result.StudentAggregates = students
                    .Select(s =>
                    {
                        var totals = result.Weeks
                            .SelectMany(w => w.Students)
                            .Where(x => x.StudentId == s.Id && x.IsActive)
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

    #endregion

    #region GetGroupPassStatsAsync

    public async Task<Response<GroupPassStatsDto>> GetGroupPassStatsAsync(int groupId, decimal threshold)
    {
        try
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null)
                return new Response<GroupPassStatsDto>(HttpStatusCode.NotFound, Messages.Journal.GroupNotFound);

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

            var aggregates = totalsResponse.Data!.StudentAggregates;
            var dto = new GroupPassStatsDto
            {
                GroupId = groupId,
                GroupName = totalsResponse.Data.GroupName,
                TotalStudents = aggregates.Count,
                PassedCount = aggregates.Count(a => (decimal)a.AveragePointsPerWeek >= threshold),
                Threshold = threshold
            };

            return new Response<GroupPassStatsDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GroupPassStatsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetGroupWeekNumbersAsync

    public async Task<Response<List<int>>> GetGroupWeekNumbersAsync(int groupId)
    {
        try
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null)
                return new Response<List<int>>(HttpStatusCode.NotFound, Messages.Journal.GroupNotFound);

            var existingWeeks = await context.Journals
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .Select(j => j.WeekNumber)
                .OrderBy(w => w)
                .ToListAsync();

            if (existingWeeks.Count == 0)
                return new Response<List<int>>(new List<int>());

            var weekNumbers = Enumerable.Range(1, existingWeeks.Max()).ToList();
            return new Response<List<int>>(weekNumbers);
        }
        catch (Exception ex)
        {
            return new Response<List<int>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region DeleteJournalAsync

    public async Task<Response<string>> DeleteJournalAsync(int groupId, int weekNumber)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var group = await GetGroupWithCenterFilterAsync(groupId, centerId);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Journal.GroupNotFound);

            var journal = await context.Journals
                .Include(j => j.Entries)
                .FirstOrDefaultAsync(j => j.GroupId == groupId && j.WeekNumber == weekNumber && !j.IsDeleted);

            if (journal == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Journal.NotFound);

            if (journal.Entries.Any())
                context.JournalEntries.RemoveRange(journal.Entries);

            context.Journals.Remove(journal);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, string.Format(Messages.Journal.WeekDeleted, weekNumber))
                : new Response<string>(HttpStatusCode.InternalServerError, Messages.Journal.WeekDeleteFailed);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region DeleteAllJournalsAsync

    public async Task<Response<string>> DeleteAllJournalsAsync(int groupId)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var group = await GetGroupWithCenterFilterAsync(groupId, centerId);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Journal.GroupNotFound);

            var journals = await context.Journals
                .Include(j => j.Entries)
                .Where(j => j.GroupId == groupId && !j.IsDeleted)
                .ToListAsync();

            if (!journals.Any())
                return new Response<string>(HttpStatusCode.NotFound, Messages.Journal.NoJournalsToDelete);

            var allEntries = journals.SelectMany(j => j.Entries).ToList();
            if (allEntries.Any())
                context.JournalEntries.RemoveRange(allEntries);

            context.Journals.RemoveRange(journals);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, string.Format(Messages.Journal.AllDeleted, journals.Count))
                : new Response<string>(HttpStatusCode.InternalServerError, Messages.Journal.AllDeleteFailed);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetStudentCommentsAsync

    public async Task<Response<List<StudentCommentDto>>> GetStudentCommentsAsync(int studentId)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

            var student = await context.Students
                .Where(s => s.Id == studentId && !s.IsDeleted)
                .FirstOrDefaultAsync();

            if (student == null)
                return new Response<List<StudentCommentDto>>(HttpStatusCode.NotFound, Messages.Student.NotFound);

            var query = context.JournalEntries
                .Include(je => je.Journal)
                    .ThenInclude(j => j!.Group)
                        .ThenInclude(g => g!.Course)
                .Where(je => je.StudentId == studentId
                    && !je.IsDeleted
                    && !string.IsNullOrWhiteSpace(je.Comment));

            if (centerId != null)
                query = query.Where(je => je.Journal!.Group!.Course!.CenterId == centerId);

            var entries = await query
                .Where(je => je.Journal != null
                    && !je.Journal.IsDeleted
                    && je.Journal.Group != null
                    && !je.Journal.Group.IsDeleted)
                .OrderByDescending(je => je.EntryDate)
                .ToListAsync();

            var comments = entries.Select(e => new StudentCommentDto
            {
                Id = e.Id,
                GroupName = e.Journal?.Group?.Name ?? "Неизвестно",
                WeekNumber = e.Journal?.WeekNumber ?? 0,
                Comment = e.Comment ?? "",
                CommentCategory = e.CommentCategory ?? CommentCategory.General,
                CommentCategoryName = GetCommentCategoryName(e.CommentCategory ?? CommentCategory.General),
                EntryDate = e.EntryDate,
                DayOfWeek = e.DayOfWeek,
                DayName = GetDayName(e.DayOfWeek),
                LessonNumber = e.LessonNumber,
                CommentAuthorName = e.CommentAuthorName
            }).ToList();

            return new Response<List<StudentCommentDto>>(comments);
        }
        catch (Exception ex)
        {
            return new Response<List<StudentCommentDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<Group?> GetGroupWithCenterFilterAsync(int groupId, int? centerId)
    {
        var query = context.Groups
            .Include(g => g.Course)
            .Where(g => !g.IsDeleted)
            .AsQueryable();

        if (centerId != null)
            query = query.Where(g => g.Course!.CenterId == centerId);

        return await query.FirstOrDefaultAsync(g => g.Id == groupId);
    }

    private async Task<bool> HasJournalAccessAsync(Journal journal, int centerId)
    {
        if (journal.Group == null)
            journal.Group = await context.Groups.Include(g => g.Course).FirstOrDefaultAsync(g => g.Id == journal.GroupId);

        if (journal.Group?.Course?.CenterId == centerId)
            return true;

        return await HasGroupAccessAsync(journal.GroupId);
    }

    private async Task<GetJournalDto> MapToJournalDtoAsync(Journal journal, bool includeDayNames = true)
    {
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
                StudentName = x.s.FullName.Trim(),
                WeeklyTotalScores = (double)x.total,
                StudentEntries = journal.Entries
                    .Where(e => e.StudentId == x.s.Id)
                    .OrderBy(e => e.LessonNumber)
                    .ThenBy(e => e.DayOfWeek)
                    .Select(e => new GetJournalEntryDto
                    {
                        Id = e.Id,
                        DayOfWeek = e.DayOfWeek,
                        DayName = includeDayNames ? GetDayNameInTajik(e.DayOfWeek) : string.Empty,
                        DayShortName = includeDayNames ? GetDayShortNameInTajik(e.DayOfWeek) : string.Empty,
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

        return new GetJournalDto
        {
            Id = journal.Id,
            GroupId = journal.GroupId,
            GroupName = journal.Group?.Name,
            WeekNumber = journal.WeekNumber,
            WeekStartDate = journal.WeekStartDate,
            WeekEndDate = journal.WeekEndDate,
            Progresses = progresses
        };
    }

    private List<(DateTime date, int dayOfWeekOneBased, int lessonNumber)> GeneratePlannedSlots(DateTime startCursor, List<int> lessonDays, int targetLessons)
    {
        var plannedSlots = new List<(DateTime date, int dayOfWeekOneBased, int lessonNumber)>();
        var cursor = startCursor;

        while (plannedSlots.Count < targetLessons)
        {
            var dotNetDayOfWeek = (int)cursor.DayOfWeek;
            var crmDayOfWeek = ConvertDotNetToCrmDayOfWeek(dotNetDayOfWeek);

            if (lessonDays.Contains(crmDayOfWeek))
                plannedSlots.Add((cursor, crmDayOfWeek, plannedSlots.Count + 1));

            cursor = cursor.AddDays(1);
        }

        return plannedSlots;
    }

    private async Task<Journal> CreateJournalWithEntriesAsync(Group group, int weekNumber, List<(DateTime date, int dayOfWeekOneBased, int lessonNumber)> plannedSlots, int targetLessons)
    {
        var firstSlotDate = plannedSlots.First().date;
        var lastSlotDate = plannedSlots.Last().date;
        var weekStart = new DateTimeOffset(firstSlotDate.Year, firstSlotDate.Month, firstSlotDate.Day, 0, 0, 0, TimeSpan.Zero);
        var weekEnd = new DateTimeOffset(lastSlotDate.Year, lastSlotDate.Month, lastSlotDate.Day, 23, 59, 59, TimeSpan.Zero);

        var journal = new Journal
        {
            GroupId = group.Id,
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
            .Where(sg => sg.GroupId == group.Id && sg.IsActive && !sg.IsDeleted && !sg.Student!.IsDeleted)
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
        return journal;
    }

    private async Task<int> BackfillStudentEntriesAsync(int journalId, int studentId)
    {
        var slots = await context.JournalEntries
            .Where(e => e.JournalId == journalId && !e.IsDeleted)
            .Select(e => new { e.DayOfWeek, e.LessonNumber, e.LessonType, e.StartTime, e.EndTime, e.EntryDate })
            .Distinct()
            .ToListAsync();

        if (slots.Count == 0) return 0;

        int created = 0;
        foreach (var s in slots)
        {
            var exists = await context.JournalEntries.AnyAsync(e =>
                e.JournalId == journalId &&
                e.StudentId == studentId &&
                e.DayOfWeek == s.DayOfWeek &&
                e.LessonNumber == s.LessonNumber &&
                !e.IsDeleted);

            if (exists) continue;

            var entry = new JournalEntry
            {
                JournalId = journalId,
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

        return created;
    }

    private async Task<bool> HasGroupAccessAsync(int groupId)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role || string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roles.Contains("SuperAdmin") || roles.Contains("Admin") || roles.Contains("Manager"))
            return true;

        var principalType = user.FindFirst("PrincipalType")?.Value;
        var idStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("nameid")?.Value;
        if (!int.TryParse(idStr, out var principalId) || principalId <= 0)
            return false;

        if (roles.Contains("Mentor") || string.Equals(principalType, "Mentor", StringComparison.OrdinalIgnoreCase))
        {
            var isPrimaryMentor = await context.Groups
                .AnyAsync(g => g.Id == groupId && !g.IsDeleted && g.MentorId == principalId);
            if (isPrimaryMentor) return true;

            return await context.MentorGroups
                .AnyAsync(mg => mg.GroupId == groupId && mg.MentorId == principalId && (mg.IsActive ?? true) && !mg.IsDeleted);
        }

        if (roles.Contains("Student") || string.Equals(principalType, "Student", StringComparison.OrdinalIgnoreCase))
        {
            return await context.StudentGroups
                .AnyAsync(sg => sg.GroupId == groupId && sg.StudentId == principalId && sg.IsActive && !sg.IsDeleted);
        }

        return false;
    }

    private static List<int> ParseLessonDays(string? lessonDays)
    {
        if (string.IsNullOrWhiteSpace(lessonDays)) return new List<int>();
        return lessonDays.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim())
            .Where(d => int.TryParse(d, out var v) && v >= 1 && v <= 7)
            .Select(int.Parse)
            .Distinct()
            .ToList();
    }

    private static int ConvertDotNetToCrmDayOfWeek(int dotNetDayOfWeek)
    {
        return dotNetDayOfWeek switch
        {
            0 => 7,
            1 => 1,
            2 => 2,
            3 => 3,
            4 => 4,
            5 => 5,
            6 => 6,
            _ => throw new ArgumentOutOfRangeException(nameof(dotNetDayOfWeek))
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
            return lessonNumber == totalLessons ? LessonType.Exam : LessonType.Regular;

        var isFourthWeekEnd = weekNumber % 4 == 0;
        var isLastLessonOfWeek = lessonNumber == totalLessons;
        return (isFourthWeekEnd && isLastLessonOfWeek) ? LessonType.Exam : LessonType.Regular;
    }

    private static string GetCommentCategoryName(CommentCategory category)
    {
        return category switch
        {
            CommentCategory.General => "Общий",
            CommentCategory.Positive => "Положительный",
            CommentCategory.Warning => "Предупреждение",
            CommentCategory.Behavior => "Поведение",
            CommentCategory.Homework => "Домашнее задание",
            CommentCategory.Participation => "Участие",
            _ => "Неизвестно"
        };
    }

    private static string GetDayName(int dayOfWeek)
    {
        return dayOfWeek switch
        {
            1 => "Понедельник",
            2 => "Вторник",
            3 => "Среда",
            4 => "Четверг",
            5 => "Пятница",
            6 => "Суббота",
            7 => "Воскресенье",
            _ => "Неизвестно"
        };
    }

    #endregion
}
