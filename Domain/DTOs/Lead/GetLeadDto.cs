using Domain.Enums;

namespace Domain.DTOs.Lead;

public class GetLeadDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public Gender Gender { get; set; }
    public OccupationStatus OccupationStatus { get; set; }
    public DateTime? RegisterForMonth { get; set; }
    public string Course { get; set; } = string.Empty;
    public TimeSpan LessonTime { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string UtmSource { get; set; } = string.Empty;
    public int CenterId { get; set; }
    public string CenterName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset UpdatedAt { get; set; }
}