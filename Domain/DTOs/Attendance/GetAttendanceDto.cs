using Domain.Enums;

namespace Domain.DTOs.Attendance;

public class GetAttendanceDto : EditAttendanceDto 
{
    public string StudentName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public DateTimeOffset LessonStartTime { get; set; }
    public int WeekIndex { get; set; }
    public int DayOfWeekIndex { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}