namespace Domain.DTOs.Payroll;

public class PayrollSummaryDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string Period => $"{Month:00}.{Year}";

    public int TotalMentors { get; set; }
    public int TotalEmployees { get; set; }

    public decimal TotalFixedAmount { get; set; }
    public decimal TotalHourlyAmount { get; set; }
    public decimal TotalPercentageAmount { get; set; }
    public decimal TotalBonusAmount { get; set; }
    public decimal TotalFineAmount { get; set; }
    public decimal TotalAdvanceDeduction { get; set; }
    public decimal TotalGrossAmount { get; set; }
    public decimal TotalNetAmount { get; set; }

    public int DraftCount { get; set; }
    public int CalculatedCount { get; set; }
    public int ApprovedCount { get; set; }
    public int PaidCount { get; set; }
}
