using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Schedule;

public class UpdateScheduleDto
{
    [Required(ErrorMessage = "ID зарур аст")]
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Интихоби синфхона зарур аст")]
    public int ClassroomId { get; set; }
    
    public int? GroupId { get; set; }
    
    [Required(ErrorMessage = "Вақти оғози дарс зарур аст")]
    public TimeOnly StartTime { get; set; }
    
    [Required(ErrorMessage = "Вақти хитоми дарс зарур аст")]
    public TimeOnly EndTime { get; set; }
    
    [Required(ErrorMessage = "Рӯзи ҳафта зарур аст")]
    public DayOfWeek DayOfWeek { get; set; }
    
    [Required(ErrorMessage = "Санаи оғоз зарур аст")]
    public DateOnly StartDate { get; set; }
    
    public DateOnly? EndDate { get; set; }
    
    public bool IsRecurring { get; set; } = true;
    
    [StringLength(500, ErrorMessage = "Изоҳот набояд аз 500 ҳарф зиёд бошад")]
    public string? Notes { get; set; }
} 