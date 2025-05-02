namespace Domain.DTOs.Grade;

public class CreateGradeDto
{
    public int GroupId { get; set; }
    public int StudentId { get; set; }
    public int LessonId { get; set; }
    public int? Value { get; set; }
    public int? BonusPoints { get; set; }
    public string? Comment { get; set; }
    public int? WeekIndex { get; set; }
}