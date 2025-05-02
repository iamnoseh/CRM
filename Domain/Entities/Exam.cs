namespace Domain.Entities;

public class Exam : BaseEntity
{
    public int? Value { get; set; }
    public string? Comment { get; set; }
    public int? BonusPoints { get; set; }
    public int WeekIndex { get; set; }
    public bool IsWeeklyExam { get; set; } = true;
    
    public DateTimeOffset ExamDate { get; set; }
    public int StudentId { get; set; }
    public int GroupId { get; set; }
    public Student Student { get; set; }
    public Group Group { get; set; }
    public List<Comment> Comments { get; set; } = new();
}