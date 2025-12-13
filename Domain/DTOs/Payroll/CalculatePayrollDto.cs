namespace Domain.DTOs.Payroll;

public class CalculatePayrollDto
{
    public int? MentorId { get; set; }
    public int? EmployeeUserId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}
