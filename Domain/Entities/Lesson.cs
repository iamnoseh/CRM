namespace Domain.Entities;

public class Lesson : BaseEntity
{
    public DateTimeOffset StartTime { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; }
    public int WeekIndex { get; set; }
    public int DayOfWeekIndex { get; set; }

    public int DayIndex { get; set; }
}