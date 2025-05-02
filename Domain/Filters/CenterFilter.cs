namespace Domain.Filters;

public class CenterFilter : BaseFilter
{
    public string? Name { get; set; }
    public string? City { get; set; }
    public int? ManagerId { get; set; }
    public bool? IsMainBranch { get; set; }
    public int? ParentCenterId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? IsActive { get; set; }
}
