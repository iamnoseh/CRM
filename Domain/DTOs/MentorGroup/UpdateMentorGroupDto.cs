namespace Domain.DTOs.MentorGroup;

public class UpdateMentorGroupDto
{
    public int? MentorId { get; set; }
    public int? GroupId { get; set; }
    public bool? IsActive { get; set; }
}
