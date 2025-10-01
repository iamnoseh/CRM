using Domain.DTOs.Mentor;

namespace Domain.DTOs.Center;

public class GetCenterMentorsDto
{
    public int CenterId { get; set; }
    public string CenterName { get; set; } = null!;
    public List<GetMentorDto> Mentors { get; set; } = new List<GetMentorDto>();
    public int TotalMentors { get; set; }
    public int ActiveMentors { get; set; }
}
