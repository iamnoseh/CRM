using Domain.Enums;

namespace Domain.DTOs.Schedule;

public class GetScheduleSimpleDto
{
    public int Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsRecurring { get; set; }
    public ActiveStatus Status { get; set; }
} 