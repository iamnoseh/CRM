using Domain.Enums;
namespace Domain.Filters;

public class CourseFilter : BaseFilter
{
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public int? DurationInMonth { get; set; }
    public ActiveStatus? Status { get; set; }
}