using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Lesson;

public class CreateLessonDto
{
    [Required]
    public int GroupId { get; set; }
    
    [Required]
    public DateTimeOffset StartTime { get; set; }
    
    [Required]
    public DateTimeOffset EndTime { get; set; }
    
    public int? ClassroomId { get; set; }
    public int? ScheduleId { get; set; }
    
    public int WeekIndex { get; set; }
    public int DayOfWeekIndex { get; set; }
    public int DayIndex { get; set; }
    
    public string? Notes { get; set; }
}