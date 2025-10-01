namespace Domain.DTOs.MentorGroup;

public class GetMentorGroupDto
{
    public int Id { get; set; }
    public int MentorId { get; set; }
    public string? MentorName { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
