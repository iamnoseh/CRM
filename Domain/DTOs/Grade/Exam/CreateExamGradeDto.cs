namespace Domain.DTOs.Grade;

public class CreateExamGradeDto
{
    public int GroupId { get; set; }
    public int StudentId { get; set; }
    public int ExamId { get; set; }
    public int DayIndex { get; set; }
    public int? Value { get; set; }
    public int? BonusPoints { get; set; }
    public string? Comment { get; set; }
    public int? WeekIndex { get; set; }
}