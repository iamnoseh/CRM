using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Journal : BaseEntity
{
    [Required]
    public int GroupId { get; set; }
    public Group? Group { get; set; }
    
    [Required]
    public int WeekNumber { get; set; }
    
    [Required]
    public DateTimeOffset WeekStartDate { get; set; }
    
    [Required]  
    public DateTimeOffset WeekEndDate { get; set; }
    
    // Navigation properties
    public List<JournalEntry> Entries { get; set; } = new();
}