using Domain.DTOs.Classroom;
using Domain.DTOs.Group;
using Domain.Enums;

namespace Domain.DTOs.Schedule;

public class GetScheduleDto
{
    public int Id { get; set; }
    public int ClassroomId { get; set; }
    public GetClassroomDto Classroom { get; set; }
    public int? GroupId { get; set; }
    public GetGroupDto? Group { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsRecurring { get; set; }
    public ActiveStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
} 