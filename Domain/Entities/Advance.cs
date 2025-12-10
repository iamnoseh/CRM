using Domain.Enums;

namespace Domain.Entities;

public class Advance : BaseEntity
{
    public int? MentorId { get; set; }
    public Mentor? Mentor { get; set; }

    public int? EmployeeUserId { get; set; }
    public User? EmployeeUser { get; set; }

    public int CenterId { get; set; }
    public Center Center { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime GivenDate { get; set; }

    public string? Reason { get; set; }

    public int TargetMonth { get; set; }
    public int TargetYear { get; set; }

    public AdvanceStatus Status { get; set; } = AdvanceStatus.Pending;

    public int? PayrollRecordId { get; set; }
    public PayrollRecord? PayrollRecord { get; set; }

    public int GivenByUserId { get; set; }
    public string? GivenByName { get; set; }
}
