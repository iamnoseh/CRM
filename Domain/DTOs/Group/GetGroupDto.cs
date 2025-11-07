using Domain.DTOs.Classroom;
using Domain.DTOs.Course;
using Domain.DTOs.Student;
using Domain.Enums;

namespace Domain.DTOs.Group;

public class GetGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMonth { get; set; }
    public int LessonInWeek { get; set; }
    public int TotalWeeks { get; set; }
    public bool Started { get; set; }
    public bool HasWeeklyExam { get; set; }
    public int CurrentStudentsCount { get; set; }
    public ActiveStatus Status { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }

    public GetSimpleDto? Mentor { get; set; }
    public int DayOfWeek { get; set; }
    public GetSimpleCourseDto? Course { get; set; }
    public string? ImagePath { get; set; }
    public int CurrentWeek { get; set; }
    public int? ClassroomId { get; set; }
    public GetClassroomDto? Classroom { get; set; }
    public string? LessonDays { get; set; }
    public TimeOnly? LessonStartTime { get; set; }
    public TimeOnly? LessonEndTime { get; set; }
}


