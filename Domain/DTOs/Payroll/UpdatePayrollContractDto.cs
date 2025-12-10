using Domain.Enums;

namespace Domain.DTOs.Payroll;

public class UpdatePayrollContractDto
{
    public SalaryType? SalaryType { get; set; }
    public decimal? FixedAmount { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? StudentPercentage { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public bool? IsActive { get; set; }
}
