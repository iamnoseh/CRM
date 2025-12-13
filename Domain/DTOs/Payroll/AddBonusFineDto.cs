namespace Domain.DTOs.Payroll;

public class AddBonusFineDto
{
    public int PayrollRecordId { get; set; }
    public decimal BonusAmount { get; set; }
    public string? BonusReason { get; set; }
    public decimal FineAmount { get; set; }
    public string? FineReason { get; set; }
}
