using Domain.DTOs.Statistics;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IFinanceService
{
    Task<Response<CenterFinancialSummaryDto>> GetFinancialSummaryAsync(int centerId, DateTimeOffset start, DateTimeOffset end);
    Task<Response<DailyFinancialSummaryDto>> GetDailySummaryAsync(int centerId, DateTimeOffset date);
    Task<Response<MonthlyFinancialSummaryDto>> GetMonthlySummaryAsync(int centerId, int year, int month);
    Task<Response<YearlyFinancialSummaryDto>> GetYearlySummaryAsync(int centerId, int year);
    Task<Response<List<CategoryAmountDto>>> GetCategoryBreakdownAsync(int centerId, DateTimeOffset start, DateTimeOffset end);
    Task<Response<int>> GenerateMentorPayrollAsync(int centerId, int year, int month);
}

