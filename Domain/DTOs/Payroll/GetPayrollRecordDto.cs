using Domain.Enums;

namespace Domain.DTOs.Payroll;

public class GetPayrollRecordDto
{
    public int Id { get; set; }
    public int? MentorId { get; set; }
    public string? MentorName { get; set; }
    public int? EmployeeUserId { get; set; }
    public string? EmployeeName { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string Period => $"{Month:00}.{Year}";

    public decimal FixedAmount { get; set; }
    public decimal HourlyAmount { get; set; }
    public decimal TotalHours { get; set; }
    public decimal PercentageAmount { get; set; }
    public decimal TotalStudentPayments { get; set; }
    public decimal PercentageRate { get; set; }
    public decimal BonusAmount { get; set; }
    public string? BonusReason { get; set; }
    public decimal FineAmount { get; set; }
    public string? FineReason { get; set; }
    public decimal AdvanceDeduction { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal NetAmount { get; set; }

    public PayrollStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public DateTime? ApprovedDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}
