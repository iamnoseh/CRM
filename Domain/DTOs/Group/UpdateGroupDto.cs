using Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Group;

public class UpdateGroupDto
{
    [Required(ErrorMessage = "Номи гурӯҳ зарур аст")]
    public required string Name { get; set; } 
    public string? Description { get; set; }
    
    [Required(ErrorMessage = "Курс интихоб кунед")]
    public int CourseId { get; set; }
    
    [Required(ErrorMessage = "Устод интихоб кунед")]  
    public int MentorId { get; set; }
    public int? ClassroomId { get; set; }
    public IFormFile? Image { get; set; }
    public bool HasWeeklyExam { get; set; } = true;
    
    [Required(ErrorMessage = "Санаи оғози дарсҳо зарур аст")]
    public DateTimeOffset? StartDate { get; set; }
    
    [Required(ErrorMessage = "Санаи анҷоми дарсҳо зарур аст")]
    public DateTimeOffset? EndDate { get; set; }

    [Required(ErrorMessage = "Рӯзҳои дарс интихоб кунед")]
    public string? LessonDays { get; set; }
    public List<int>? ParsedLessonDays 
    {
        get 
        {
            if (string.IsNullOrEmpty(LessonDays))
                return null;
                
            return LessonDays.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim())
                .Where(d => int.TryParse(d, out var dayInt) && dayInt >= 0 && dayInt <= 6)
                .Select(int.Parse)
                .Distinct()
                .ToList();
        }
    }
    
    [Required(ErrorMessage = "Вақти оғози дарс зарур аст")]
    public TimeOnly? LessonStartTime { get; set; }
    
    [Required(ErrorMessage = "Вақти анҷоми дарс зарур аст")]
    public TimeOnly? LessonEndTime { get; set; }
    
    public bool AutoGenerateLessons { get; set; } = true;
    public int? DurationMonth => StartDate.HasValue && EndDate.HasValue 
        ? (int)Math.Ceiling((EndDate.Value - StartDate.Value).TotalDays / 30.0) 
        : null;
        
    public int? LessonInWeek => ParsedLessonDays?.Count;
}