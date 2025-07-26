using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Classroom : BaseEntity
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public int? Capacity { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [Required]
    public int CenterId { get; set; }
    public Center Center { get; set; }
    
    public List<Lesson> Lessons { get; set; } = new();
    public List<Schedule> Schedules { get; set; } = new();
} 