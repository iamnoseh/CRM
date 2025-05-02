using Domain.DTOs.Student;

namespace Domain.DTOs.Mentor;

public class UpdateMentorDto : CreateMentorDto
{
    public int Id { get; set; }
}