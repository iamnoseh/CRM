using Domain.DTOs.Statistics;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Infrastructure.Helpers;
using Serilog;

namespace Infrastructure.Services;

public class FinanceService(DataContext dbContext, IHttpContextAccessor httpContextAccessor) : IFinanceService
{
    private readonly DataContext _db = dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<Response<CenterFinancialSummaryDto>> GetFinancialSummaryAsync(int centerId, DateTimeOffset start, DateTimeOffset end)
    {
        try
        {
            var userCenterId = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            var effectiveCenterId = userCenterId ?? centerId;
            var center = await _db.Centers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == effectiveCenterId);
            if (center is null)
                return new Response<CenterFinancialSummaryDto>(System.Net.HttpStatusCode.NotFound, "Центр не найден");

            var income = await _db.Payments
                .Where(p => p.CenterId == effectiveCenterId
                            && p.PaymentDate >= start.UtcDateTime && p.PaymentDate <= end.UtcDateTime
                            && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid))
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var expense = await _db.Expenses
                .Where(e => e.CenterId == effectiveCenterId
                            && e.ExpenseDate >= start && e.ExpenseDate <= end
                            && !e.IsDeleted)
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var byCategory = await _db.Expenses
                .Where(e => e.CenterId == effectiveCenterId && e.ExpenseDate >= start && e.ExpenseDate <= end && !e.IsDeleted)
                .GroupBy(e => e.Category)
                .Select(g => new CategoryAmountDto
                {
                    Category = g.Key.ToString(),
                    Amount = g.Sum(x => x.Amount)
                })
                .ToListAsync();

            // By day trend
            var incomesByDay = await _db.Payments
                .Where(p => p.CenterId == effectiveCenterId
                            && p.PaymentDate >= start.UtcDateTime && p.PaymentDate <= end.UtcDateTime
                            && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid))
                .GroupBy(p => p.PaymentDate.Date)
                .Select(g => new { Date = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToListAsync();

            var expensesByDay = await _db.Expenses
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
            Log.Error(ex, "Ошибка при расчёте финансового отчёта для центра {CenterId}", centerId);
            return new Response<CenterFinancialSummaryDto>(System.Net.HttpStatusCode.InternalServerError, "Не удалось рассчитать сводку");
        }
    }

    public async Task<Response<DailyFinancialSummaryDto>> GetDailySummaryAsync(int centerId, DateTimeOffset date)
    {
        var start = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, date.Offset);
        var end = start.AddDays(1).AddTicks(-1);
        var summary = await GetFinancialSummaryAsync(centerId, start, end);
        if (summary.Data == null)
            return new Response<DailyFinancialSummaryDto>((System.Net.HttpStatusCode)summary.StatusCode, summary.Message ?? "");

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

    public async Task<Response<MonthlyFinancialSummaryDto>> GetMonthlySummaryAsync(int centerId, int year, int month)
    {
        try
        {
            var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddMonths(1).AddTicks(-1);
            var summary = await GetFinancialSummaryAsync(centerId, start, end);
            if (summary.Data == null)
                return new Response<MonthlyFinancialSummaryDto>((System.Net.HttpStatusCode)summary.StatusCode, summary.Message ?? "");

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
            Log.Error(ex, "Ошибка при расчёте месячной сводки {Year}-{Month} для центра {CenterId}", year, month, centerId);
            return new Response<MonthlyFinancialSummaryDto>(System.Net.HttpStatusCode.InternalServerError, "Не удалось рассчитать месячную сводку");
        }
    }

    public async Task<Response<YearlyFinancialSummaryDto>> GetYearlySummaryAsync(int centerId, int year)
    {
        try
        {
            var start = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddYears(1).AddTicks(-1);
            var summary = await GetFinancialSummaryAsync(centerId, start, end);
            if (summary.Data == null)
                return new Response<YearlyFinancialSummaryDto>((System.Net.HttpStatusCode)summary.StatusCode, summary.Message ?? "");

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
            Log.Error(ex, "Ошибка при расчёте годовой сводки {Year} для центра {CenterId}", year, centerId);
            return new Response<YearlyFinancialSummaryDto>(System.Net.HttpStatusCode.InternalServerError, "Не удалось рассчитать годовую сводку");
        }
    }

    public async Task<Response<List<CategoryAmountDto>>> GetCategoryBreakdownAsync(int centerId, DateTimeOffset start, DateTimeOffset end)
    {
        try
        {
            var breakdown = await _db.Expenses
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
            Log.Error(ex, "Ошибка при расчёте разбивки по категориям для центра {CenterId}", centerId);
            return new Response<List<CategoryAmountDto>>(System.Net.HttpStatusCode.InternalServerError, "Не удалось рассчитать разбивку по категориям");
        }
    }

    public async Task<Response<int>> GenerateMentorPayrollAsync(int centerId, int year, int month)
    {
        try
        {
            var centerExists = await _db.Centers.AnyAsync(c => c.Id == centerId);
            if (!centerExists)
                return new Response<int>(System.Net.HttpStatusCode.NotFound, "Центр не найден");

            var mentors = await _db.Mentors
                .Where(m => m.CenterId == centerId && m.ActiveStatus == ActiveStatus.Active)
                .Select(m => new { m.Id, m.Salary })
                .ToListAsync();

            var createdCount = 0;
            foreach (var mentor in mentors)
            {
                var exists = await _db.Expenses.AnyAsync(e => e.CenterId == centerId
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
                _db.Expenses.Add(expense);
                createdCount++;
            }

            if (createdCount > 0)
                await _db.SaveChangesAsync();

            var user = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";
            Log.Information("Пользователь {User} сформировал начисление зарплат для центра {CenterId} {Year}-{Month}: {Count} записей", user, centerId, year, month, createdCount);
            return new Response<int>(createdCount);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при формировании начисления зарплат для центра {CenterId} {Year}-{Month}", centerId, year, month);
            return new Response<int>(System.Net.HttpStatusCode.InternalServerError, "Не удалось сформировать начисление зарплат");
        }
    }
}


