using Domain.Enums;

namespace Domain.DTOs.Payroll;

public class GetPayrollContractDto
{
    public int Id { get; set; }
    public int? MentorId { get; set; }
    public string? MentorName { get; set; }
    public int? EmployeeUserId { get; set; }
    public string? EmployeeName { get; set; }
    public SalaryType SalaryType { get; set; }
    public string SalaryTypeDisplay { get; set; } = string.Empty;
    public decimal FixedAmount { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal StudentPercentage { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
