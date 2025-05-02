using Domain.Enums;

namespace Domain.DTOs.Group;

public class GetGroupDto
{
    public int Id { get; set; }
    public string? Name { get; set; } 
    public string? Description { get; set; }
    public int CourseId { get; set; }
    public int DurationMonth { get; set; }
    public int LessonInWeek { get; set; }
    public int TotalWeeks { get; set; }
    public bool Started { get; set; }
    public int CurrentStudentsCount { get; set; }
    public ActiveStatus Status { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public int MentorId { get; set; }
    public int DayOfWeek { get; set; }
    public string? ImagePath { get; set; }
    public int CurrentWeek { get; set; }
}