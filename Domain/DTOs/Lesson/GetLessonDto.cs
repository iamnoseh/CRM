namespace Domain.DTOs.Lesson;

public class GetLessonDto
{
    public int Id { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public int GroupId { get; set; }
    public int WeekIndex { get; set; }
    public int DayOfWeekIndex { get; set; }
    public int DayIndex { get; set; }
    public string GroupName { get; set; } = string.Empty;
}