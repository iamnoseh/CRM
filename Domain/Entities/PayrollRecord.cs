using Domain.Enums;

namespace Domain.Entities;

public class PayrollRecord : BaseEntity
{
    public int? MentorId { get; set; }
    public Mentor? Mentor { get; set; }

    public int? EmployeeUserId { get; set; }
    public User? EmployeeUser { get; set; }

    public int CenterId { get; set; }
    public Center Center { get; set; } = null!;

    public int Month { get; set; }
    public int Year { get; set; }

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

    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;

    public DateTime? ApprovedDate { get; set; }
    public int? ApprovedByUserId { get; set; }

    public DateTime? PaidDate { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }

    public string? Notes { get; set; }
}
