using Domain.Enums;

namespace Domain.Entities;

public class PayrollContract : BaseEntity
{
    public int? MentorId { get; set; }
    public Mentor? Mentor { get; set; }

    public int? EmployeeUserId { get; set; }
    public User? EmployeeUser { get; set; }

    public int CenterId { get; set; }
    public Center Center { get; set; } = null!;

    public SalaryType SalaryType { get; set; }

    public decimal FixedAmount { get; set; }

    public decimal HourlyRate { get; set; }

    public decimal StudentPercentage { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}
