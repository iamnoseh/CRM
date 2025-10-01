namespace Domain.DTOs.Journal;

public class StudentWeekTotalsDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal TotalPoints { get; set; }
}