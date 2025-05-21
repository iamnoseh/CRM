namespace Domain.DTOs.Lesson;

public class CreateLessonDto
{
    public int GroupId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public int WeekIndex { get; set; }
    public int DayOfWeekIndex { get; set; }
    
    /// <summary>
    /// Индекси умумии рӯз барои дарс (бе вобастагӣ аз ҳафта)
    /// Барои пайвасткунии бо модели Grade истифода мешавад
    /// </summary>
    public int DayIndex { get; set; }
}