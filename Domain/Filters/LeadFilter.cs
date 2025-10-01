using Domain.Enums;

namespace Domain.Filters;

public class LeadFilter : BaseFilter
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public Gender? Gender { get; set; }
    public OccupationStatus? OccupationStatus { get; set; }
    public DateTime? RegisterForMonth { get; set; }
    public string? Course { get; set; }
    public string? UtmSource { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}