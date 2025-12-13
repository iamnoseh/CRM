using Domain.Enums;

namespace Domain.Filters;

public class PayrollFilter : BaseFilter
{
    public int? MentorId { get; set; }
    public int? EmployeeUserId { get; set; }
    public string? Search { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public PayrollStatus? Status { get; set; }
}
