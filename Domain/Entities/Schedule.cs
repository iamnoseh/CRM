using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;
public class Schedule : BaseEntity
{
    [Required]
    public int ClassroomId { get; set; }
    public Classroom? Classroom { get; set; }
    
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
    
    [Required]
    public TimeOnly StartTime { get; set; }
    
    [Required]
    public TimeOnly EndTime { get; set; }
    
    [Required]
    public DayOfWeek DayOfWeek { get; set; }
    
    [Required]
    public DateOnly StartDate { get; set; }
    
    public DateOnly? EndDate { get; set; }
    public bool IsRecurring { get; set; } = true;
    public ActiveStatus Status { get; set; } = ActiveStatus.Active;
    
    [StringLength(500)]
    public string? Notes { get; set; }
}