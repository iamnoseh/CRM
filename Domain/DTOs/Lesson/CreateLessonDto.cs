namespace Domain.DTOs.Lesson;

public class CreateLessonDto
{
    public int GroupId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public int WeekIndex { get; set; }
    public int DayOfWeekIndex { get; set; }
}