using Domain.DTOs.Statistics;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Infrastructure.Services;

public class PaymentStatisticsService(DataContext db,
    IHttpContextAccessor httpContextAccessor) : IPaymentStatisticsService
{
    private readonly DataContext _db = db;
    private readonly IHttpContextAccessor _http = httpContextAccessor;

    private static bool IsPaid(Payment p) => p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid;
    private static bool IsUnpaid(Payment p) => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Failed;

    public async Task<Response<StudentPaymentStatisticsDto>> GetStudentPaymentStatisticsAsync(
        int studentId,
        int? groupId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_http);

            var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == studentId);
            if (student is null)
                return new Response<StudentPaymentStatisticsDto>(System.Net.HttpStatusCode.NotFound, "Донишҷӯ ёфт нашуд");
            if (userCenterId != null && student.CenterId != userCenterId)
                return new Response<StudentPaymentStatisticsDto>(System.Net.HttpStatusCode.Forbidden, "Дастрасӣ манъ аст");

            Group? group = null;
            if (groupId.HasValue)
            {
                group = await _db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId.Value);
                if (group is null)
                    return new Response<StudentPaymentStatisticsDto>(System.Net.HttpStatusCode.NotFound, "Гурӯҳ ёфт нашуд");
            }

            // Default period: current month when not provided
            if (!startDate.HasValue || !endDate.HasValue)
            {
                var now = DateTimeOffset.UtcNow;
                var first = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
                var last = first.AddMonths(1).AddTicks(-1);
                startDate ??= first;
                endDate ??= last;
            }
            var query = _db.Payments.AsNoTracking().Where(p => p.StudentId == studentId);
            if (groupId.HasValue)
                query = query.Where(p => p.GroupId == groupId.Value);
            if (startDate.HasValue)
                query = query.Where(p => p.PaymentDate >= startDate.Value.UtcDateTime);
            if (endDate.HasValue)
                query = query.Where(p => p.PaymentDate <= endDate.Value.UtcDateTime);

            var payments = await query.ToListAsync();

            var totalAmount = payments.Sum(p => p.Amount);
            var paidAmount = payments.Where(IsPaid).Sum(p => p.Amount);
            var unpaidAmount = payments.Where(IsUnpaid).Sum(p => p.Amount);
            var recent = payments.OrderByDescending(p => p.PaymentDate)
                .Take(10)
                .Select(p => new PaymentDetailDto
                {
                    PaymentId = p.Id,
                    StudentId = p.StudentId,
                    StudentName = student.FullName,
                    Amount = p.Amount,
                    PaymentDate = new DateTimeOffset(p.PaymentDate, TimeSpan.Zero),
                    PaymentMethod = p.PaymentMethod.ToString()
                }).ToList();

            var result = new StudentPaymentStatisticsDto
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                GroupId = groupId ?? 0,
                GroupName = group?.Name ?? "Ҳама",
                TotalAmount = totalAmount,
                PaidAmount = paidAmount,
                UnpaidAmount = unpaidAmount,
                TotalPayments = payments.Count,
                StartDate = startDate!.Value,
                EndDate = endDate!.Value,
                RecentPayments = recent
            };

            return new Response<StudentPaymentStatisticsDto>(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Хато ҳангоми гирифтани статистикаи пардохти донишҷӯ {StudentId}", studentId);
            return new Response<StudentPaymentStatisticsDto>(System.Net.HttpStatusCode.InternalServerError, "Хато ҳангоми ҳисоб");
        }
    }

    public async Task<Response<List<GroupPaymentStatisticsDto>>> GetDailyGroupPaymentStatisticsAsync(int groupId, DateTimeOffset date)
    {
        var start = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, date.Offset);
        var end = start.AddDays(1).AddTicks(-1);
        var one = await GetGroupPaymentStatisticsAsync(groupId, start, end);
        if (one.Data == null)
            return new Response<List<GroupPaymentStatisticsDto>>((System.Net.HttpStatusCode)one.StatusCode, one.Message ?? "");
        return new Response<List<GroupPaymentStatisticsDto>>(new List<GroupPaymentStatisticsDto> { one.Data });
    }

    public async Task<Response<GroupPaymentStatisticsDto>> GetGroupPaymentStatisticsAsync(int groupId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        try
        {
            var group = await _db.Groups.AsNoTracking().Include(g => g.Course).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group is null)
                return new Response<GroupPaymentStatisticsDto>(System.Net.HttpStatusCode.NotFound, "Гурӯҳ ёфт нашуд");

            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_http);
            if (userCenterId != null && group.Course != null)
            {
                var centerIdOfGroup = group.Course.CenterId;
                if (centerIdOfGroup != userCenterId)
                    return new Response<GroupPaymentStatisticsDto>(System.Net.HttpStatusCode.Forbidden, "Дастрасӣ манъ аст");
            }

            var query = _db.Payments.AsNoTracking().Where(p => p.GroupId == groupId);
            // Default period: current month if none provided
            if (!startDate.HasValue || !endDate.HasValue)
            {
                var now = DateTimeOffset.UtcNow;
                var first = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
                var last = first.AddMonths(1).AddTicks(-1);
                startDate ??= first;
                endDate ??= last;
            }
            if (startDate.HasValue)
                query = query.Where(p => p.PaymentDate >= startDate.Value.UtcDateTime);
            if (endDate.HasValue)
                query = query.Where(p => p.PaymentDate <= endDate.Value.UtcDateTime);

            var payments = await query.ToListAsync();
            var total = payments.Sum(p => p.Amount);
            var paid = payments.Where(IsPaid).Sum(p => p.Amount);
            var unpaid = payments.Where(IsUnpaid).Sum(p => p.Amount);

            var totalStudents = await _db.StudentGroups.AsNoTracking().CountAsync(sg => sg.GroupId == groupId && sg.IsActive);

            var studentIds = await _db.StudentGroups.AsNoTracking().Where(sg => sg.GroupId == groupId && sg.IsActive)
                .Select(sg => sg.StudentId).ToListAsync();
            var unpaidStudents = new List<StudentPaymentStatisticsDto>();
            if (studentIds.Count > 0)
            {
                var studentPayments = await _db.Payments.AsNoTracking()
                    .Where(p => p.GroupId == groupId && studentIds.Contains(p.StudentId))
                    .ToListAsync();
                var groupsByStudent = studentPayments.GroupBy(p => p.StudentId);
                foreach (var g in groupsByStudent)
                {
                    var unpaidAmount = g.Where(IsUnpaid).Sum(x => x.Amount);
                    if (unpaidAmount > 0)
                    {
                        var sid = g.Key;
                        var s = await _db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sid);
                        if (s != null)
                        {
                            unpaidStudents.Add(new StudentPaymentStatisticsDto
                            {
                                StudentId = sid,
                                StudentName = s.FullName,
                                GroupId = groupId,
                                GroupName = group.Name,
                                TotalAmount = g.Sum(x => x.Amount),
                                PaidAmount = g.Where(IsPaid).Sum(x => x.Amount),
                                UnpaidAmount = unpaidAmount,
                                TotalPayments = g.Count(),
                                StartDate = startDate ?? (g.Count() > 0 ? new DateTimeOffset(g.Min(x => x.PaymentDate), TimeSpan.Zero) : DateTimeOffset.MinValue),
                                EndDate = endDate ?? (g.Count() > 0 ? new DateTimeOffset(g.Max(x => x.PaymentDate), TimeSpan.Zero) : DateTimeOffset.MinValue),
                                RecentPayments = g.OrderByDescending(x => x.PaymentDate).Take(5).Select(x => new PaymentDetailDto
                                {
                                    PaymentId = x.Id,
                                    StudentId = x.StudentId,
                                    StudentName = s.FullName,
                                    Amount = x.Amount,
                                    PaymentDate = new DateTimeOffset(x.PaymentDate, TimeSpan.Zero),
                                    PaymentMethod = x.PaymentMethod.ToString()
                                }).ToList()
                            });
                        }
                    }
                }
            }

            var recent = payments.OrderByDescending(p => p.PaymentDate).Take(10).Select(p => new PaymentDetailDto
            {
                PaymentId = p.Id,
                StudentId = p.StudentId,
                StudentName = _db.Students.Where(s => s.Id == p.StudentId).Select(s => s.FullName).FirstOrDefault() ?? "",
                Amount = p.Amount,
                PaymentDate = new DateTimeOffset(p.PaymentDate, TimeSpan.Zero),
                PaymentMethod = p.PaymentMethod.ToString()
            }).ToList();

            var dto = new GroupPaymentStatisticsDto
            {
                GroupId = group.Id,
                GroupName = group.Name,
                TotalStudents = totalStudents,
                TotalAmount = total,
                PaidAmount = paid,
                UnpaidAmount = unpaid,
                TotalPayments = payments.Count,
                StartDate = startDate!.Value,
                EndDate = endDate!.Value,
                RecentPayments = recent,
                UnpaidStudents = unpaidStudents
            };
            return new Response<GroupPaymentStatisticsDto>(dto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Хато ҳангоми ҳисобкунии статистикаи гурӯҳ {GroupId}", groupId);
            return new Response<GroupPaymentStatisticsDto>(System.Net.HttpStatusCode.InternalServerError, "Хато ҳангоми ҳисоб");
        }
    }

    public async Task<Response<GroupPaymentStatisticsDto>> GetMonthlyGroupPaymentStatisticsAsync(int groupId, int year, int month)
    {
        var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1).AddTicks(-1);
        return await GetGroupPaymentStatisticsAsync(groupId, start, end);
    }

    public async Task<Response<List<CenterPaymentStatisticsDto>>> GetDailyCenterPaymentStatisticsAsync(int centerId, DateTimeOffset date)
    {
        var start = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, date.Offset);
        var end = start.AddDays(1).AddTicks(-1);
        var one = await GetCenterPaymentStatisticsAsync(centerId, start, end);
        if (one.Data == null)
            return new Response<List<CenterPaymentStatisticsDto>>((System.Net.HttpStatusCode)one.StatusCode, one.Message ?? "");
        return new Response<List<CenterPaymentStatisticsDto>>(new List<CenterPaymentStatisticsDto> { one.Data });
    }

    public async Task<Response<CenterPaymentStatisticsDto>> GetCenterPaymentStatisticsAsync(int centerId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_http);
            var effectiveCenterId = userCenterId ?? centerId;
            var center = await _db.Centers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == effectiveCenterId);
            if (center is null)
                return new Response<CenterPaymentStatisticsDto>(System.Net.HttpStatusCode.NotFound, "Марказ ёфт нашуд");

            var paymentsQuery = _db.Payments.AsNoTracking().Where(p => p.CenterId == effectiveCenterId);
            if (startDate.HasValue)
                paymentsQuery = paymentsQuery.Where(p => p.PaymentDate >= startDate.Value.UtcDateTime);
            if (endDate.HasValue)
                paymentsQuery = paymentsQuery.Where(p => p.PaymentDate <= endDate.Value.UtcDateTime);
            var payments = await paymentsQuery.ToListAsync();

            var total = payments.Sum(p => p.Amount);
            var paid = payments.Where(IsPaid).Sum(p => p.Amount);
            var unpaid = payments.Where(IsUnpaid).Sum(p => p.Amount);

            // Total groups and students for center
            var totalGroups = await _db.Groups.AsNoTracking().Include(g => g.Course)
                .CountAsync(g => g.Course != null && g.Course.CenterId == effectiveCenterId);
            var totalStudents = await _db.Students.AsNoTracking().CountAsync(s => s.CenterId == effectiveCenterId);

            // Group breakdown
            var groupStats = new List<GroupPaymentStatisticsDto>();
            var groupIds = payments.Where(p => p.GroupId != null).Select(p => p.GroupId!.Value).Distinct().ToList();
            if (groupIds.Count > 0)
            {
                foreach (var gid in groupIds)
                {
                    var g = await GetGroupPaymentStatisticsAsync(gid, startDate, endDate);
                    if (g.Data != null)
                        groupStats.Add(g.Data);
                }
            }

            var dto = new CenterPaymentStatisticsDto
            {
                CenterId = center.Id,
                CenterName = center.Name,
                TotalGroups = totalGroups,
                TotalStudents = totalStudents,
                TotalAmount = total,
                PaidAmount = paid,
                UnpaidAmount = unpaid,
                TotalPayments = payments.Count,
                StartDate = startDate ?? (payments.Count > 0 ? new DateTimeOffset(payments.Min(x => x.PaymentDate), TimeSpan.Zero) : DateTimeOffset.MinValue),
                EndDate = endDate ?? (payments.Count > 0 ? new DateTimeOffset(payments.Max(x => x.PaymentDate), TimeSpan.Zero) : DateTimeOffset.MinValue),
                GroupStatistics = groupStats
            };
            return new Response<CenterPaymentStatisticsDto>(dto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Хато ҳангоми ҳисобкунии статистикаи марказ {CenterId}", centerId);
            return new Response<CenterPaymentStatisticsDto>(System.Net.HttpStatusCode.InternalServerError, "Хато ҳангоми ҳисоб");
        }
    }

    public async Task<Response<CenterPaymentStatisticsDto>> GetMonthlyCenterPaymentStatisticsAsync(int centerId, int year, int month)
    {
        var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1).AddTicks(-1);
        return await GetCenterPaymentStatisticsAsync(centerId, start, end);
    }
}


