namespace Domain.Entities;
public class Grade : BaseEntity
{
    public int GroupId { get; set; }
    public int StudentId { get; set; }
    public int? LessonId { get; set; }
    public int ExamId { get; set; }
    public int? Value { get; set; }
    public string? Comment { get; set; }
    public int? BonusPoints { get; set; }
    public int? WeekIndex { get; set; }
    public int? DayIndex { get; set; }
    public Student Student { get; set; }
    public Group Group { get; set; }
    public Lesson? Lesson { get; set; }
    public Exam? Exam { get; set; }
}