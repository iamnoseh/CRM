using System.Net;
using Domain.DTOs.Statistics;
using Domain.DTOs.Finance;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Constants;
using Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Infrastructure.Services;

public class FinanceService(DataContext dbContext, IHttpContextAccessor httpContextAccessor) : IFinanceService
{
    #region GetFinancialSummaryAsync

    public async Task<Response<CenterFinancialSummaryDto>> GetFinancialSummaryAsync(int centerId, DateTimeOffset start, DateTimeOffset end)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var effectiveCenterId = userCenterId ?? centerId;
            var center = await dbContext.Centers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == effectiveCenterId);
            if (center is null)
                return new Response<CenterFinancialSummaryDto>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            var income = await dbContext.Payments
                .Where(p => p.CenterId == effectiveCenterId
                            && p.PaymentDate >= start.UtcDateTime && p.PaymentDate <= end.UtcDateTime
                            && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid))
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var expense = await dbContext.Expenses
                .Where(e => e.CenterId == effectiveCenterId
                            && e.ExpenseDate >= start && e.ExpenseDate <= end
                            && !e.IsDeleted)
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var byCategory = await dbContext.Expenses
                .Where(e => e.CenterId == effectiveCenterId && e.ExpenseDate >= start && e.ExpenseDate <= end && !e.IsDeleted)
                .GroupBy(e => e.Category)
                .Select(g => new CategoryAmountDto
                {
                    Category = g.Key.ToString(),
                    Amount = g.Sum(x => x.Amount)
                })
                .ToListAsync();

            var incomesByDay = await dbContext.Payments
                .Where(p => p.CenterId == effectiveCenterId
                            && p.PaymentDate >= start.UtcDateTime && p.PaymentDate <= end.UtcDateTime
                            && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid))
                .GroupBy(p => p.PaymentDate.Date)
                .Select(g => new { Date = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToListAsync();

            var expensesByDay = await dbContext.Expenses
                .Where(e => e.CenterId == effectiveCenterId && e.ExpenseDate >= start && e.ExpenseDate <= end && !e.IsDeleted)
                .GroupBy(e => e.ExpenseDate.Date)
                .Select(g => new { Date = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToListAsync();

            var byDay = new List<DailyAmountDto>();
            var dateCursor = start.UtcDateTime.Date;
            var endDate = end.UtcDateTime.Date;
            while (dateCursor <= endDate)
            {
                var incomeAmount = incomesByDay.FirstOrDefault(x => x.Date == dateCursor)?.Amount ?? 0m;
                var expenseAmount = expensesByDay.FirstOrDefault(x => x.Date == dateCursor)?.Amount ?? 0m;
                byDay.Add(new DailyAmountDto
                {
                    Date = new DateTimeOffset(dateCursor, TimeSpan.Zero),
                    Income = incomeAmount,
                    Expense = expenseAmount
                });
                dateCursor = dateCursor.AddDays(1);
            }

            var result = new CenterFinancialSummaryDto
            {
                CenterId = center.Id,
                CenterName = center.Name,
                StartDate = start,
                EndDate = end,
                IncomeTotal = income,
                ExpenseTotal = expense,
                ByExpenseCategory = byCategory,
                ByDay = byDay
            };

            return new Response<CenterFinancialSummaryDto>(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, Messages.Finance.LogSummaryError, centerId);
            return new Response<CenterFinancialSummaryDto>(HttpStatusCode.InternalServerError, Messages.Finance.SummaryCalculationFailed);
        }
    }

    #endregion

    #region GetDailySummaryAsync

    public async Task<Response<DailyFinancialSummaryDto>> GetDailySummaryAsync(int centerId, DateTimeOffset date)
    {
        var start = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, date.Offset);
        var end = start.AddDays(1).AddTicks(-1);
        var summary = await GetFinancialSummaryAsync(centerId, start, end);
        if (summary.Data == null)
            return new Response<DailyFinancialSummaryDto>((HttpStatusCode)summary.StatusCode, summary.Message ?? "");

        var dto = new DailyFinancialSummaryDto
        {
            IncomeTotal = summary.Data.IncomeTotal,
            ExpenseTotal = summary.Data.ExpenseTotal,
            StartDate = summary.Data.StartDate,
            EndDate = summary.Data.EndDate,
            ByExpenseCategory = summary.Data.ByExpenseCategory,
            ByDay = summary.Data.ByDay
        };
        return new Response<DailyFinancialSummaryDto>(dto);
    }

    #endregion

    #region GetMonthlySummaryAsync

    public async Task<Response<MonthlyFinancialSummaryDto>> GetMonthlySummaryAsync(int centerId, int year, int month)
    {
        try
        {
            var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddMonths(1).AddTicks(-1);
            var summary = await GetFinancialSummaryAsync(centerId, start, end);
            if (summary.Data == null)
                return new Response<MonthlyFinancialSummaryDto>((HttpStatusCode)summary.StatusCode, summary.Message ?? "");

            var dto = new MonthlyFinancialSummaryDto
            {
                Year = year,
                Month = month,
                IncomeTotal = summary.Data.IncomeTotal,
                ExpenseTotal = summary.Data.ExpenseTotal,
                StartDate = summary.Data.StartDate,
                EndDate = summary.Data.EndDate,
                ByExpenseCategory = summary.Data.ByExpenseCategory,
                ByDay = summary.Data.ByDay
            };
            return new Response<MonthlyFinancialSummaryDto>(dto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, Messages.Finance.LogMonthlySummaryError, year, month, centerId);
            return new Response<MonthlyFinancialSummaryDto>(HttpStatusCode.InternalServerError, Messages.Finance.MonthlySummaryFailed);
        }
    }

    #endregion

    #region GetYearlySummaryAsync

    public async Task<Response<YearlyFinancialSummaryDto>> GetYearlySummaryAsync(int centerId, int year)
    {
        try
        {
            var start = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddYears(1).AddTicks(-1);
            var summary = await GetFinancialSummaryAsync(centerId, start, end);
            if (summary.Data == null)
                return new Response<YearlyFinancialSummaryDto>((HttpStatusCode)summary.StatusCode, summary.Message ?? "");

            var dto = new YearlyFinancialSummaryDto
            {
                Year = year,
                IncomeTotal = summary.Data.IncomeTotal,
                ExpenseTotal = summary.Data.ExpenseTotal,
                StartDate = summary.Data.StartDate,
                EndDate = summary.Data.EndDate,
                ByExpenseCategory = summary.Data.ByExpenseCategory,
                ByDay = summary.Data.ByDay
            };
            return new Response<YearlyFinancialSummaryDto>(dto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, Messages.Finance.LogYearlySummaryError, year, centerId);
            return new Response<YearlyFinancialSummaryDto>(HttpStatusCode.InternalServerError, Messages.Finance.YearlySummaryFailed);
        }
    }

    #endregion

    #region GetCategoryBreakdownAsync

    public async Task<Response<List<CategoryAmountDto>>> GetCategoryBreakdownAsync(int centerId, DateTimeOffset start, DateTimeOffset end)
    {
        try
        {
            var breakdown = await dbContext.Expenses
                .Where(e => e.CenterId == centerId && e.ExpenseDate >= start && e.ExpenseDate <= end && !e.IsDeleted)
                .GroupBy(e => e.Category)
                .Select(g => new CategoryAmountDto
                {
                    Category = g.Key.ToString(),
                    Amount = g.Sum(x => x.Amount)
                })
                .ToListAsync();
            return new Response<List<CategoryAmountDto>>(breakdown);
        }
        catch (Exception ex)
        {
            Log.Error(ex, Messages.Finance.LogCategoryBreakdownError, centerId);
            return new Response<List<CategoryAmountDto>>(HttpStatusCode.InternalServerError, Messages.Finance.CategoryBreakdownFailed);
        }
    }

    #endregion

    #region GenerateMentorPayrollAsync

    public async Task<Response<int>> GenerateMentorPayrollAsync(int centerId, int year, int month)
    {
        try
        {
            var centerExists = await dbContext.Centers.AnyAsync(c => c.Id == centerId);
            if (!centerExists)
                return new Response<int>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            var mentors = await dbContext.Mentors
                .Where(m => m.CenterId == centerId && m.ActiveStatus == ActiveStatus.Active)
                .Select(m => new { m.Id, m.Salary })
                .ToListAsync();

            var createdCount = 0;
            foreach (var mentor in mentors)
            {
                var exists = await dbContext.Expenses.AnyAsync(e => e.CenterId == centerId
                                                              && e.MentorId == mentor.Id
                                                              && e.Category == ExpenseCategory.Salary
                                                              && e.Month == month && e.Year == year
                                                              && !e.IsDeleted);
                if (exists) continue;

                var expenseDate = new DateTimeOffset(new DateTime(year, month, DateTime.DaysInMonth(year, month), 0, 0, 0, DateTimeKind.Utc));
                var expense = new Expense
                {
                    CenterId = centerId,
                    Amount = mentor.Salary,
                    ExpenseDate = expenseDate,
                    Category = ExpenseCategory.Salary,
                    PaymentMethod = PaymentMethod.Other,
                    Description = $"Ежемесячная зарплата преподавателя #{mentor.Id} ({year}-{month:00})",
                    MentorId = mentor.Id,
                    Month = month,
                    Year = year,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                dbContext.Expenses.Add(expense);
                createdCount++;
            }

            if (createdCount > 0)
                await dbContext.SaveChangesAsync();

            var user = httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";
            Log.Information(Messages.Finance.LogPayrollGenerationSuccess, user, centerId, year, month, createdCount);
            return new Response<int>(createdCount);
        }
        catch (Exception ex)
        {
            Log.Error(ex, Messages.Finance.LogPayrollGenerationError, centerId, year, month);
            return new Response<int>(HttpStatusCode.InternalServerError, Messages.Finance.PayrollGenerationFailed);
        }
    }

    #endregion

    #region GetDebtsAsync

    public async Task<Response<List<DebtDto>>> GetDebtsAsync(int centerId, int year, int month, int? studentId)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var effectiveCenterId = userCenterId ?? centerId;

            var studentGroups = dbContext.StudentGroups
                .AsNoTracking()
                .Where(sg => !sg.IsDeleted)
                .Join(dbContext.Groups.AsNoTracking(), sg => sg.GroupId, g => g.Id, (sg, g) => new { sg, g })
                .Join(dbContext.Courses.AsNoTracking(), x => x.g.CourseId, c => c.Id, (x, c) => new { x.sg, x.g, c })
                .Where(x => x.c.CenterId == effectiveCenterId);

            if (studentId.HasValue)
                studentGroups = studentGroups.Where(x => x.sg.StudentId == studentId.Value);

            var discounts = dbContext.StudentGroupDiscounts.AsNoTracking().ToList();

            var list = await studentGroups
                .Select(x => new
                {
                    x.sg.StudentId,
                    x.sg.GroupId,
                    x.g.Name,
                    x.c.Price
                })
                .ToListAsync();

            var studentNames = await dbContext.Students.AsNoTracking()
                .Where(s => list.Select(i => i.StudentId).Contains(s.Id))
                .Select(s => new { s.Id, s.FullName })
                .ToListAsync();

            var payments = await dbContext.Payments.AsNoTracking()
                .Where(p => p.CenterId == effectiveCenterId && p.Year == year && p.Month == month && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid))
                .GroupBy(p => new { p.StudentId, p.GroupId })
                .Select(g => new { g.Key.StudentId, g.Key.GroupId, Amount = g.Sum(p => p.Amount) })
                .ToListAsync();

            var result = new List<DebtDto>();
            foreach (var item in list)
            {
                var discount = discounts.FirstOrDefault(d => d.StudentId == item.StudentId && d.GroupId == item.GroupId)?.DiscountAmount ?? 0m;
                var expected = item.Price - discount;
                if (expected < 0) expected = 0;
                var paid = payments.FirstOrDefault(p => p.StudentId == item.StudentId && p.GroupId == item.GroupId)?.Amount ?? 0m;
                var balance = expected - paid;
                if (balance <= 0) continue;
                var st = studentNames.FirstOrDefault(s => s.Id == item.StudentId);
                result.Add(new DebtDto
                {
                    StudentId = item.StudentId,
                    StudentName = st?.FullName,
                    GroupId = item.GroupId,
                    GroupName = item.Name,
                    OriginalAmount = item.Price,
                    DiscountAmount = discount,
                    PaidAmount = paid,
                    Balance = balance,
                    Month = month,
                    Year = year
                });
            }

            return new Response<List<DebtDto>>(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, Messages.Finance.LogDebtCalculationError, centerId, year, month);
            return new Response<List<DebtDto>>(HttpStatusCode.InternalServerError, Messages.Finance.DebtCalculationFailed);
        }
    }

    #endregion

    #region SetMonthClosedAsync

    public async Task<Response<bool>> SetMonthClosedAsync(int centerId, int year, int month, bool isClosed)
    {
        try
        {
            var centerExists = await dbContext.Centers.AnyAsync(c => c.Id == centerId);
            if (!centerExists)
                return new Response<bool>(HttpStatusCode.NotFound, Messages.Center.NotFound);

            var summary = await dbContext.MonthlyFinancialSummaries
                .FirstOrDefaultAsync(m => m.CenterId == centerId && m.Year == year && m.Month == month);

            if (summary is null)
            {
                var income = await dbContext.Payments.AsNoTracking()
                    .Where(p => p.CenterId == centerId && p.Year == year && p.Month == month && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid))
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;
                var expense = await dbContext.Expenses.AsNoTracking()
                    .Where(e => e.CenterId == centerId && e.Year == year && e.Month == month && !e.IsDeleted)
                    .SumAsync(e => (decimal?)e.Amount) ?? 0m;
                summary = new MonthlyFinancialSummary
                {
                    CenterId = centerId,
                    Year = year,
                    Month = month,
                    TotalIncome = income,
                    TotalExpense = expense,
                    NetProfit = income - expense,
                    GeneratedDate = DateTimeOffset.UtcNow,
                    IsClosed = isClosed,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                dbContext.MonthlyFinancialSummaries.Add(summary);
            }
            else
            {
                summary.IsClosed = isClosed;
                summary.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await dbContext.SaveChangesAsync();
            return new Response<bool>(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, Messages.Finance.LogMonthCloseError, year, month, centerId);
            return new Response<bool>(HttpStatusCode.InternalServerError, Messages.Finance.OperationFailed);
        }
    }

    #endregion

    #region IsMonthClosedAsync

    public async Task<bool> IsMonthClosedAsync(int centerId, int year, int month, CancellationToken ct = default)
    {
        var closed = await dbContext.MonthlyFinancialSummaries
            .AsNoTracking()
            .Where(m => m.CenterId == centerId && m.Year == year && m.Month == month)
            .Select(m => (bool?)m.IsClosed)
            .FirstOrDefaultAsync(ct);
        return closed ?? false;
    }

    #endregion
}
