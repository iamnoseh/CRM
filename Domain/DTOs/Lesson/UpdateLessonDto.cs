namespace Domain.DTOs.Lesson;

public class UpdateLessonDto
{
    public int LessonId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public int WeekIndex { get; set; }
    public int DayOfWeekIndex { get; set; }
}