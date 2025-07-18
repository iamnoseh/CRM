using System.Net;
using Domain.DTOs.Statistics;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class PaymentStatisticsService(DataContext context, IHttpContextAccessor httpContextAccessor) : IPaymentStatisticsService
{
    public async Task<Response<StudentPaymentStatisticsDto>> GetStudentPaymentStatisticsAsync(
        int studentId,
        int? groupId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            var student = await context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);

            if (student == null)
                return new Response<StudentPaymentStatisticsDto>(HttpStatusCode.NotFound, "Student not found");            var query = context.Payments
                .Include(p => p.Student)
                .Include(p => p.Group)
                    .ThenInclude(g => g.Course)
                .Include(p => p.Center)
                .Where(p => p.StudentId == studentId && 
                           !p.IsDeleted && 
                           p.Student != null && 
                           !p.Student.IsDeleted);

            if (groupId.HasValue)
                query = query.Where(p => p.GroupId == groupId && 
                                       p.Group != null && 
                                       !p.Group.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(p => p.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.CreatedAt <= endDate.Value);

            var payments = await query.ToListAsync();            
            var statistics = new StudentPaymentStatisticsDto
            {
                StudentId = studentId,
                StudentName = student.FullName,
                GroupId = groupId ?? 0,
                GroupName = groupId.HasValue ? payments.FirstOrDefault()?.Group?.Name ?? "Unknown" : "All Groups",
                TotalAmount = payments.Sum(p => p.Amount),
                PaidAmount = payments.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount),
                UnpaidAmount = payments.Where(p => p.Status != PaymentStatus.Paid).Sum(p => p.Amount),
                TotalPayments = payments.Count,
                StartDate = startDate ?? payments.Min(p => p.PaymentDate),
                EndDate = endDate ?? payments.Max(p => p.PaymentDate),
                RecentPayments = payments.OrderByDescending(p => p.PaymentDate)
                    .Take(5)                    .Select(p => new PaymentDetailDto
                    {
                        PaymentId = p.Id,
                        StudentId = p.StudentId,
                        StudentName = p.Student?.FullName ?? student.FullName,
                        Amount = p.Amount,
                        PaymentDate = p.PaymentDate,
                        PaymentMethod = p.PaymentMethod.ToString()
                    }).ToList()
            };

            return new Response<StudentPaymentStatisticsDto>(statistics);
        }
        catch (Exception ex)
        {
            return new Response<StudentPaymentStatisticsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<GroupPaymentStatisticsDto>> GetGroupPaymentStatisticsAsync(
        int groupId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

            if (group == null)
                return new Response<GroupPaymentStatisticsDto>(HttpStatusCode.NotFound, "Group not found");            var query = context.Payments
                .Include(p => p.Student)
                .Include(p => p.Group)
                .Include(p => p.Center)
                .Where(p => p.GroupId == groupId && !p.IsDeleted && !p.Group.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(p => p.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.CreatedAt <= endDate.Value);

            var payments = await query.ToListAsync();            var studentPayments = payments
                .GroupBy(p => new { p.StudentId, StudentName = p.Student.FullName })
                .Select(g => new StudentPaymentStatisticsDto
                {
                    StudentId = g.Key.StudentId,
                    StudentName = g.Key.StudentName,
                    GroupId = groupId,
                    GroupName = group.Name,
                    TotalAmount = g.Sum(p => p.Amount),
                    PaidAmount = g.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount),
                    UnpaidAmount = g.Where(p => p.Status != PaymentStatus.Paid).Sum(p => p.Amount),
                    TotalPayments = g.Count(),
                    StartDate = startDate ?? g.Min(p => p.PaymentDate),
                    EndDate = endDate ?? g.Max(p => p.PaymentDate)
                })
                .ToList();            var statistics = new GroupPaymentStatisticsDto
            {
                GroupId = groupId,
                GroupName = group.Name,
                TotalStudents = studentPayments.Count,
                TotalAmount = payments.Sum(p => p.Amount),
                PaidAmount = payments.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount),
                UnpaidAmount = payments.Where(p => p.Status != PaymentStatus.Paid).Sum(p => p.Amount),
                TotalPayments = payments.Count,
                StartDate = startDate ?? payments.Min(p => p.PaymentDate),
                EndDate = endDate ?? payments.Max(p => p.PaymentDate),
                UnpaidStudents = studentPayments
                    .Where(s => s.UnpaidAmount > 0)
                    .OrderByDescending(s => s.UnpaidAmount)
                    .ToList(),                RecentPayments = payments
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(5)
                    .Select(p => new PaymentDetailDto
                    {
                        PaymentId = p.Id,
                        StudentId = p.StudentId,
                        StudentName = p.Student.FullName,
                        Amount = p.Amount,
                        PaymentDate = p.PaymentDate,
                        PaymentMethod = p.PaymentMethod.ToString()
                    }).ToList()
            };

            return new Response<GroupPaymentStatisticsDto>(statistics);
        }
        catch (Exception ex)
        {
            return new Response<GroupPaymentStatisticsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<List<GroupPaymentStatisticsDto>>> GetDailyGroupPaymentStatisticsAsync(
        int groupId,
        DateTimeOffset date)
    {
        try
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var result = await GetGroupPaymentStatisticsAsync(groupId, startDate, endDate);
            return new Response<List<GroupPaymentStatisticsDto>>(
                new List<GroupPaymentStatisticsDto> { result.Data });
        }
        catch (Exception ex)
        {
            return new Response<List<GroupPaymentStatisticsDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<GroupPaymentStatisticsDto>> GetMonthlyGroupPaymentStatisticsAsync(
        int groupId,
        int year,
        int month)
    {
        try
        {
            var startDate = new DateTimeOffset(new DateTime(year, month, 1));
            var endDate = startDate.AddMonths(1);

            return await GetGroupPaymentStatisticsAsync(groupId, startDate, endDate);
        }
        catch (Exception ex)
        {
            return new Response<GroupPaymentStatisticsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<CenterPaymentStatisticsDto>> GetCenterPaymentStatisticsAsync(
        int centerId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        var userCenterId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
        var user = httpContextAccessor.HttpContext?.User;
        var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");
        if (!isSuperAdmin && userCenterId != centerId)
            return new Response<CenterPaymentStatisticsDto>(System.Net.HttpStatusCode.Forbidden, "Access denied to this center's statistics");
        try
        {
            var center = await context.Centers
                .FirstOrDefaultAsync(c => c.Id == centerId && !c.IsDeleted);

            if (center == null)
                return new Response<CenterPaymentStatisticsDto>(HttpStatusCode.NotFound, "Center not found");            var groups = await context.Groups
                .Include(g => g.Course)
                .Where(g => g.Course != null && g.Course.CenterId == centerId && !g.IsDeleted)
                .ToListAsync();

            var groupStatistics = new List<GroupPaymentStatisticsDto>();

            foreach (var group in groups)
            {
                var groupStats = await GetGroupPaymentStatisticsAsync(group.Id, startDate, endDate);
                if (groupStats.StatusCode == (int)HttpStatusCode.OK)
                {
                    groupStatistics.Add(groupStats.Data);
                }
            }

            var statistics = new CenterPaymentStatisticsDto
            {
                CenterId = centerId,
                CenterName = center.Name,
                TotalGroups = groupStatistics.Count,
                TotalStudents = groupStatistics.Sum(g => g.TotalStudents),
                TotalAmount = groupStatistics.Sum(g => g.TotalAmount),
                PaidAmount = groupStatistics.Sum(g => g.PaidAmount),
                UnpaidAmount = groupStatistics.Sum(g => g.UnpaidAmount),
                TotalPayments = groupStatistics.Sum(g => g.TotalPayments),
                StartDate = startDate ?? groupStatistics.Min(g => g.StartDate),
                EndDate = endDate ?? groupStatistics.Max(g => g.EndDate),
                GroupStatistics = groupStatistics
            };

            return new Response<CenterPaymentStatisticsDto>(statistics);
        }
        catch (Exception ex)
        {
            return new Response<CenterPaymentStatisticsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<List<CenterPaymentStatisticsDto>>> GetDailyCenterPaymentStatisticsAsync(
        int centerId,
        DateTimeOffset date)
    {
        var userCenterId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
        var user = httpContextAccessor.HttpContext?.User;
        var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");
        if (!isSuperAdmin && userCenterId != centerId)
            return new Response<List<CenterPaymentStatisticsDto>>(System.Net.HttpStatusCode.Forbidden, "Access denied to this center's statistics");
        try
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var result = await GetCenterPaymentStatisticsAsync(centerId, startDate, endDate);
            return new Response<List<CenterPaymentStatisticsDto>>(
                new List<CenterPaymentStatisticsDto> { result.Data });
        }
        catch (Exception ex)
        {
            return new Response<List<CenterPaymentStatisticsDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    public async Task<Response<CenterPaymentStatisticsDto>> GetMonthlyCenterPaymentStatisticsAsync(
        int centerId,
        int year,
        int month)
    {
        var userCenterId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
        var user = httpContextAccessor.HttpContext?.User;
        var roles = user?.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");
        if (!isSuperAdmin && userCenterId != centerId)
            return new Response<CenterPaymentStatisticsDto>(System.Net.HttpStatusCode.Forbidden, "Access denied to this center's statistics");
        try
        {
            var startDate = new DateTimeOffset(new DateTime(year, month, 1));
            var endDate = startDate.AddMonths(1);

            return await GetCenterPaymentStatisticsAsync(centerId, startDate, endDate);
        }
        catch (Exception ex)
        {
            return new Response<CenterPaymentStatisticsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
