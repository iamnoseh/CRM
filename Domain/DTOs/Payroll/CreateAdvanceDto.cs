namespace Domain.DTOs.Payroll;

public class CreateAdvanceDto
{
    public int? MentorId { get; set; }
    public int? EmployeeUserId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public int TargetMonth { get; set; }
    public int TargetYear { get; set; }
}
