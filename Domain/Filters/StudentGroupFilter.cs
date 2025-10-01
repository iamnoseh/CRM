using Domain.Enums;

namespace Domain.Filters;

public class StudentGroupFilter : BaseFilter
{
    public string? Search { get; set; }
    public int? StudentId { get; set; }
    public int? GroupId { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? JoinedDateFrom { get; set; }
    public DateTime? JoinedDateTo { get; set; }
} 