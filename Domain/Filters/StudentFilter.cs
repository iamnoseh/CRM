using Domain.Enums;

namespace Domain.Filters;

public class StudentFilter : BaseFilter
{
    public string? FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public ActiveStatus? Active { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public Gender? Gender { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int? GroupId { get; set; }
    public int? CourseId { get; set; }
    public DateTime? JoinedDateFrom { get; set; }
    public DateTime? JoinedDateTo { get; set; }
    public int? CenterId { get; set; }
    public bool? IsActive { get; set; }
}

public class StudentFilterForSelect : BaseFilter
{
    public string? FullName { get; set; }
}