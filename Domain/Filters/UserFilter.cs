using Domain.Enums;
namespace Domain.Filters;

public class UserFilter : BaseFilter
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Role? Role { get; set; }
}