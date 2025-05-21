using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.Group;

public class CreateGroupDto
{
    public required string Name { get; set; } 
    public string? Description { get; set; }
    public int CourseId { get; set; }
    public int DurationMonth { get; set; }
    public int LessonInWeek { get; set; } = 5;
    public bool HasWeeklyExam { get; set; } = true;
    public int MentorId { get; set; }
    
    public IFormFile? Image { get; set; }
}