using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;
public class Group : BaseEntity
{
    [Required]
    public required string Name { get; set; } 
    public string? Description { get; set; }
    [Required]
    public int CourseId { get; set; }
    [Required]
    public int DurationMonth { get; set; }
    public int LessonInWeek { get; set; } = 5;
    public bool HasWeeklyExam { get; set; } = true;
    public int TotalWeeks { get; set; }
    public bool Started { get; set; }
    public ActiveStatus Status { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public string? PhotoPath { get; set; }
    public int MentorId { get; set; }
    public Mentor Mentor { get; set; }
    public Course Course { get; set; }
    public int? ClassroomId { get; set; }
    public Classroom? Classroom { get; set; }
    public List<StudentGroup> StudentGroups { get; set; } = new();
    public List<Lesson> Lessons { get; set; } = new();
    public List<Schedule> Schedules { get; set; } = new();
    public int CurrentWeek { get; set; } = 1;
    public string? LessonDays { get; set; } 
    public TimeOnly? LessonStartTime { get; set; }
    public TimeOnly? LessonEndTime { get; set; }
}