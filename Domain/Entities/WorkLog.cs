namespace Domain.Entities;

public class WorkLog : BaseEntity
{
    public int? MentorId { get; set; }
    public Mentor? Mentor { get; set; }

    public int? EmployeeUserId { get; set; }
    public User? EmployeeUser { get; set; }

    public int CenterId { get; set; }
    public Center Center { get; set; } = null!;

    public DateTime WorkDate { get; set; }

    public decimal Hours { get; set; }

    public string? Description { get; set; }

    public int? GroupId { get; set; }
    public Group? Group { get; set; }

    public int Month { get; set; }
    public int Year { get; set; }
}
