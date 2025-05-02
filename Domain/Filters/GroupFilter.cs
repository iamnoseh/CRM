using Domain.Enums;

namespace Domain.Filters;

public class GroupFilter : BaseFilter
{
    public string? Name { get; set; }
    public int? CourseId { get; set; }
    public int? MentorId { get; set; }
    public bool? Started { get; set; }
    public ActiveStatus? Status { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public DateTime? EndDateFrom { get; set; }
    public DateTime? EndDateTo { get; set; }
}