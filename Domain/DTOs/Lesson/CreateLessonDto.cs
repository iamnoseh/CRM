using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Lesson;

public class CreateLessonDto
{
    [Required(ErrorMessage = "Интихоби гурӯҳ зарур аст")]
    public int GroupId { get; set; }
    
    [Required(ErrorMessage = "Вақти оғоз зарур аст")]
    public DateTimeOffset StartTime { get; set; }
    
    [Required(ErrorMessage = "Вақти хитом зарур аст")]
    public DateTimeOffset EndTime { get; set; }
    
    public int? ClassroomId { get; set; }
    public int? ScheduleId { get; set; }
    
    public int WeekIndex { get; set; }
    public int DayOfWeekIndex { get; set; }
    public int DayIndex { get; set; }
    
    [StringLength(500, ErrorMessage = "Изоҳот набояд аз 500 ҳарф зиёд бошад")]
    public string? Notes { get; set; }
}