using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Lesson : BaseEntity
{
    [Required]
    public DateTimeOffset StartTime { get; set; }
    [Required]
    public DateTimeOffset EndTime { get; set; }
    [Required]
    public int GroupId { get; set; }
    public Group Group { get; set; }
    public int? ClassroomId { get; set; }
    public Classroom? Classroom { get; set; }
    public int? ScheduleId { get; set; }
    public Schedule? Schedule { get; set; }
    
    public int WeekIndex { get; set; }
    public int DayOfWeekIndex { get; set; }
    public int DayIndex { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
}