namespace Domain.Entities;

public class MonthlyFinancialSummary : BaseEntity
{
    public int CenterId { get; set; }
    public Center Center { get; set; }

    public int Month { get; set; }
    public int Year { get; set; }

    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit { get; set; }

    public DateTimeOffset GeneratedDate { get; set; }
    public bool IsClosed { get; set; }
}

