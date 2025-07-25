using Domain.Enums;

namespace Domain.Filters;

public class MentorFilter : BaseFilter
{
    public int? Id { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public int? Age { get; set; }
    public Gender? Gender { get; set; }
    public decimal? Salary { get; set; }
    public int? CenterId { get; set; } 
}