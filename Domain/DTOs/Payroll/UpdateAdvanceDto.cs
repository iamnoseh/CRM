namespace Domain.DTOs.Payroll;

public class UpdateAdvanceDto
{
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public int TargetMonth { get; set; }
    public int TargetYear { get; set; }
}
