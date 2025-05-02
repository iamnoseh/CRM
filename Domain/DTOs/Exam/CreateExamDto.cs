namespace Domain.DTOs.Exam;

public class CreateExamDto
{
    public int? Value { get; set; }
    public int? BonusPoints { get; set; }
    public string? Comment { get; set; }
    public int StudentId { get; set; }
    public int GroupId { get; set; }
    public int WeekIndex { get; set; }
    public DateTimeOffset ExamDate { get; set; } = DateTime.Now;
}