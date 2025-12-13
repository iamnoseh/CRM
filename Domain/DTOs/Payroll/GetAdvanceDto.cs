using Domain.Enums;

namespace Domain.DTOs.Payroll;

public class GetAdvanceDto
{
    public int Id { get; set; }
    public int? MentorId { get; set; }
    public string? MentorName { get; set; }
    public int? EmployeeUserId { get; set; }
    public string? EmployeeName { get; set; }
    public decimal Amount { get; set; }
    public DateTime GivenDate { get; set; }
    public string? Reason { get; set; }
    public int TargetMonth { get; set; }
    public int TargetYear { get; set; }
    public string TargetPeriod => $"{TargetMonth:00}.{TargetYear}";
    public AdvanceStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public string? GivenByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
