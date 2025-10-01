namespace Domain.DTOs.Statistics;

public class CategoryAmountDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class DailyAmountDto
{
    public DateTimeOffset Date { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}

public class FinancialSummaryDto
{
    public decimal IncomeTotal { get; set; }
    public decimal ExpenseTotal { get; set; }
    public decimal NetProfit => IncomeTotal - ExpenseTotal;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public List<CategoryAmountDto> ByExpenseCategory { get; set; } = new();
    public List<DailyAmountDto> ByDay { get; set; } = new();
}

public class CenterFinancialSummaryDto : FinancialSummaryDto
{
    public int CenterId { get; set; }
    public required string CenterName { get; set; }
}

public class DailyFinancialSummaryDto : FinancialSummaryDto
{
}

public class MonthlyFinancialSummaryDto : FinancialSummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
}

public class YearlyFinancialSummaryDto : FinancialSummaryDto
{
    public int Year { get; set; }
}

