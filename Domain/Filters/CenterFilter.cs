namespace Domain.Filters;

public class CenterFilter : BaseFilter
{
    public string? Name { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? IsActive { get; set; }
}
