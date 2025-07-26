using Domain.DTOs.Schedule;
using Domain.Enums;

namespace Domain.DTOs.Classroom;

public class GetClassroomScheduleDto
{
    public int ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string CenterName { get; set; } = string.Empty;
    public List<GetScheduleSimpleDto> Schedules { get; set; } = new();
    public List<TimeSlotDto> AvailableTimeSlots { get; set; } = new();
}

public class TimeSlotDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public string? OccupiedBy { get; set; } // Group name if occupied
} 