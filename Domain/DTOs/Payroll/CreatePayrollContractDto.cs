using Domain.Enums;

namespace Domain.DTOs.Payroll;

public class CreatePayrollContractDto
{
    public int? MentorId { get; set; }
    public int? EmployeeUserId { get; set; }
    public SalaryType SalaryType { get; set; }
    public decimal FixedAmount { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal StudentPercentage { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}
