namespace Domain.DTOs.Schedule;

public class ScheduleConflictDto
{
    public bool HasConflict { get; set; }
    public List<ConflictDetail> Conflicts { get; set; } = new();
    public List<TimeSlotSuggestion> Suggestions { get; set; } = new();
}

public class ConflictDetail
{
    public int ScheduleId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class TimeSlotSuggestion
{
    public int ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsPreferred { get; set; } // если это в том же классе что и запрашивали
} 