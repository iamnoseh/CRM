using System.Net;
using Domain.DTOs.Student;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Infrastructure.Features.Students.Queries.GetStudentGroupsOverview;

public class GetStudentGroupsOverviewHandler(
    DataContext context,
    IHttpContextAccessor httpContextAccessor,
    IJournalService journalService)
    : IRequestHandler<GetStudentGroupsOverviewQuery, Response<List<StudentGroupOverviewDto>>>
{
    public async Task<Response<List<StudentGroupOverviewDto>>> Handle(GetStudentGroupsOverviewQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var studentId = request.StudentId;
            
            Log.Information("Получение обзора групп для студента {StudentId}", studentId);
            
            var studentsQuery = context.Students.Where(s => !s.IsDeleted && s.Id == studentId);
            studentsQuery =
                QueryFilterHelper.FilterByCenterIfNotSuperAdmin(studentsQuery, httpContextAccessor, s => s.CenterId);
            var student = await studentsQuery.Select(s => new { s.Id }).FirstOrDefaultAsync(cancellationToken);
            
            if (student == null)
            {
                Log.Warning("Студент {StudentId} не найден или доступ запрещен", studentId);
                return new Response<List<StudentGroupOverviewDto>>(HttpStatusCode.Forbidden,
                    "Дастрасӣ манъ аст ё донishҷӯ ёфт нашуд");
            }

            var groupItems = await context.Groups
                .Where(g => !g.IsDeleted &&
                            g.StudentGroups.Any(sg => !sg.IsDeleted && sg.IsActive && sg.StudentId == studentId))
                .Select(g => new
                {
                    GroupId = g.Id,
                    GroupName = g.Name,
                    GroupImagePath = g.PhotoPath,
                    CourseName = g.Course != null ? g.Course.CourseName : null,
                    CourseImagePath = g.Course != null ? g.Course.ImagePath : null
                })
                .ToListAsync(cancellationToken);

            if (groupItems.Count == 0)
            {
                Log.Information("Активные группы для студента {StudentId} не найдены", studentId);
                return new Response<List<StudentGroupOverviewDto>>(new List<StudentGroupOverviewDto>());
            }

            var groupIds = groupItems.Select(x => x.GroupId).ToList();
            Log.Information("Студент {StudentId} имеет {GroupCount} активных групп", studentId, groupIds.Count);

            // ✅ Fix N+1: Pre-load all discounts in one query
            var discountsByGroup = await context.StudentGroupDiscounts
                .Where(x => x.StudentId == studentId && groupIds.Contains(x.GroupId) && !x.IsDeleted)
                .GroupBy(x => x.GroupId)
                .Select(g => new 
                {
                    GroupId = g.Key,
                    DiscountAmount = g.OrderByDescending(x => x.UpdatedAt)
                                      .Select(x => x.DiscountAmount)
                                      .FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.GroupId, x => x.DiscountAmount, cancellationToken);

            // Weekly average per group (includes exam days in journal entries)
            var weeklyAvgByGroup = new Dictionary<int, double>();
            foreach (var gid in groupIds)
            {
                var wk = await journalService.GetGroupWeeklyTotalsAsync(gid, null);
                var avg = wk.Data?.StudentAggregates?.FirstOrDefault(a => a.StudentId == studentId)
                    ?.AveragePointsPerWeek;
                if (avg.HasValue) weeklyAvgByGroup[gid] = avg.Value;
            }

            // determine payments this month per group
            var nowUtc = DateTime.UtcNow;
            var paidThisMonthIds = await context.Payments
                .Where(p => !p.IsDeleted &&
                            p.StudentId == studentId &&
                            p.GroupId != null &&
                            groupIds.Contains(p.GroupId!.Value) &&
                            p.Year == nowUtc.Year &&
                            p.Month == nowUtc.Month &&
                            (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid))
                .Select(p => p.GroupId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
            var paidThisMonthSet = new HashSet<int>(paidThisMonthIds);

            var latestPaymentsByGroup = await context.Payments
                .Where(p => !p.IsDeleted && p.StudentId == studentId && p.GroupId != null &&
                            groupIds.Contains(p.GroupId.Value))
                .GroupBy(p => p.GroupId!.Value)
                .Select(g => g
                    .OrderByDescending(p => p.PaymentDate)
                    .ThenByDescending(p => p.CreatedAt)
                    .Select(p => new { GroupId = p.GroupId!.Value, p.Status, p.PaymentDate })
                    .FirstOrDefault())
                .ToListAsync(cancellationToken);

            var paymentsDict = latestPaymentsByGroup
                .Where(x => x != null)
                .ToDictionary(x => x!.GroupId, x => x);

            var journalStats = await context.Journals
                .Where(j => !j.IsDeleted && groupIds.Contains(j.GroupId))
                .Join(
                    context.JournalEntries.Where(e =>
                        !e.IsDeleted && e.StudentId == studentId && e.EntryDate <= nowUtc),
                    j => j.Id,
                    e => e.JournalId,
                    (j, e) => new { j.GroupId, Entry = e })
                .GroupBy(x => x.GroupId)
                .Select(g => new
                {
                    GroupId = g.Key,
                    PresentCount = g.Count(x => x.Entry.AttendanceStatus == AttendanceStatus.Present),
                    LateCount = g.Count(x => x.Entry.AttendanceStatus == AttendanceStatus.Late),
                    AbsentCount = g.Count(x => x.Entry.AttendanceStatus == AttendanceStatus.Absent),
                    SumScore = g.Sum(x => (x.Entry.Grade ?? 0m) + (x.Entry.BonusPoints ?? 0m)),
                    ScoredCount = g.Count(x => x.Entry.Grade != null || x.Entry.BonusPoints != null)
                })
                .ToListAsync(cancellationToken);

            var journalDict = journalStats.ToDictionary(x => x.GroupId, x => x);

            // cache course prices for groups
            var priceByGroupId = await context.Groups
                .Include(g => g.Course)
                .Where(g => groupIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Course != null ? g.Course.Price : 0m, cancellationToken);

            var result = new List<StudentGroupOverviewDto>();
            foreach (var g in groupItems)
            {
                paymentsDict.TryGetValue(g.GroupId, out var pay);

                // effective payment status for current month
                PaymentStatus? paymentStatus;
                if (paidThisMonthSet.Contains(g.GroupId))
                {
                    paymentStatus = PaymentStatus.Completed;
                }
                else
                {
                    // ✅ Use pre-loaded discounts
                    var price = priceByGroupId.TryGetValue(g.GroupId, out var p) ? p : 0m;
                    var discount = discountsByGroup.TryGetValue(g.GroupId, out var d) ? d : 0m;
                    var applied = Math.Min(price, discount);
                    var net = price - applied;
                    paymentStatus = net <= 0 ? PaymentStatus.Completed : PaymentStatus.Pending;
                }

                journalDict.TryGetValue(g.GroupId, out var stat);
                var totalEntries = (stat?.PresentCount ?? 0) + (stat?.LateCount ?? 0) + (stat?.AbsentCount ?? 0);
                var attendanceRate = totalEntries > 0
                    ? Math.Round((decimal)(stat!.PresentCount) * 100m / totalEntries, 2)
                    : 0m;
                var averageScore = weeklyAvgByGroup.TryGetValue(g.GroupId, out var avgWeek)
                    ? (decimal)Math.Round(avgWeek, 2)
                    : 0m;

                result.Add(new StudentGroupOverviewDto
                {
                    GroupId = g.GroupId,
                    GroupName = g.GroupName,
                    GroupImagePath = g.GroupImagePath,
                    CourseName = g.CourseName,
                    CourseImagePath = g.CourseImagePath,
                    PaymentStatus = paymentStatus,
                    LastPaymentDate = pay?.PaymentDate,
                    AverageScore = averageScore,
                    AttendanceRatePercent = attendanceRate,
                    PresentCount = stat?.PresentCount ?? 0,
                    LateCount = stat?.LateCount ?? 0,
                    AbsentCount = stat?.AbsentCount ?? 0
                });
            }

            Log.Information("Обзор групп завершен для студента {StudentId}: {GroupCount} групп", 
                studentId, result.Count);

            return new Response<List<StudentGroupOverviewDto>>(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка получения обзора групп для студента {StudentId}", request.StudentId);
            return new Response<List<StudentGroupOverviewDto>>(HttpStatusCode.InternalServerError, 
                "Хатогӣ ҳангоми гирифтани маълумот");
        }
    }
}
