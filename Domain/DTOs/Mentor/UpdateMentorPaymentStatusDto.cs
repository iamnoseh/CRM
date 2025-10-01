namespace Domain.DTOs.Mentor;

public class UpdateMentorPaymentStatusDto
{
    public int MentorId { get; set; }
    public Enums.PaymentStatus Status { get; set; }
}