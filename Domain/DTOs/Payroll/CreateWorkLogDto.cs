namespace Domain.DTOs.Payroll;

public class CreateWorkLogDto
{
    public int? MentorId { get; set; }
    public int? EmployeeUserId { get; set; }
    public DateTime WorkDate { get; set; }
    public decimal Hours { get; set; }
    public string? Description { get; set; }
    public int? GroupId { get; set; }
}
