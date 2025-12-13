namespace Domain.DTOs.Payroll;

public class GetWorkLogDto
{
    public int Id { get; set; }
    public int? MentorId { get; set; }
    public string? MentorName { get; set; }
    public int? EmployeeUserId { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime WorkDate { get; set; }
    public decimal Hours { get; set; }
    public string? Description { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}
