using Domain.Enums;

namespace Domain.Filters;

public class ExpenseFilter : BaseFilter
{
    public int CenterId { get; set; }
    public ExpenseCategory? Category { get; set; }
    public int? MentorId { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public string? Search { get; set; }
}