namespace Domain.Filters;

public class BaseFilter
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string? SortBy { get; set; }
    public bool SortAscending { get; set; } = true;
    
    public BaseFilter()
    {
        PageNumber = 1;
        PageSize = 10;
    }
    
    public BaseFilter(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize < 10 ? 10 : pageSize;
    }    
}