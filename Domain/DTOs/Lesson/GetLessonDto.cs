using Domain.DTOs.Classroom;
using Domain.DTOs.Group;
using Domain.DTOs.Schedule;

namespace Domain.DTOs.Lesson;

public class GetLessonDto
{
    public int Id { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    
    public int GroupId { get; set; }
    public GetGroupDto Group { get; set; }
    
    public int? ClassroomId { get; set; }
    public GetClassroomDto? Classroom { get; set; }
    
    public int? ScheduleId { get; set; }
    public GetScheduleDto? Schedule { get; set; }
    
    public int WeekIndex { get; set; }
    public int DayOfWeekIndex { get; set; }
    public int DayIndex { get; set; }
    public string? Notes { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}