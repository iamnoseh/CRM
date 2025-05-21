namespace Domain.Entities;

public class Lesson : BaseEntity
{
    public DateTimeOffset StartTime { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; }
    public List<Grade> Grades { get; set; } = new();
    public List<Attendance> Attendances { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public int WeekIndex { get; set; }
    public int DayOfWeekIndex { get; set; }
    
    /// <summary>
    /// Индекси умумии рӯз барои дарс (бе вобастагӣ аз ҳафта)
    /// Формулаи ҳисоб: (WeekIndex - 1) * 5 + DayOfWeekIndex
    /// Барои пайвасткунии дуруст бо модели Grade истифода мешавад
    /// </summary>
    public int DayIndex { get; set; }
}