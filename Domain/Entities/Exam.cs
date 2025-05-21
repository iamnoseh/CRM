namespace Domain.Entities;

/// <summary>
/// Сущность экзамена для группы
/// </summary>
public class Exam : BaseEntity
{
    public int WeekIndex { get; set; }
    public DateTimeOffset ExamDate { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; }
    public List<Grade> Grades { get; set; }
}