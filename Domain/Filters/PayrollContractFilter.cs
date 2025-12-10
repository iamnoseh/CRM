using Domain.Enums;

namespace Domain.Filters;

public class PayrollContractFilter : BaseFilter
{
    public int? MentorId { get; set; }
    public int? EmployeeUserId { get; set; }
    public string? Search {get;set;}
    public SalaryType? SalaryType { get; set; }
    public bool? IsActive { get; set; }
}
