using Domain.DTOs.Student;
using Domain.Enums;

namespace Domain.DTOs.Mentor;

public class UpdateMentorDto : CreateMentorDto
{
    public int Id { get; set; }
    public int CenterId { get; set; }
    public ActiveStatus ActiveStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
}