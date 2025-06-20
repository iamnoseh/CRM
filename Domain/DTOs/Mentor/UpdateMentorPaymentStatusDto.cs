namespace Domain.DTOs.Mentor;

public class UpdateMentorPaymentStatusDto
{
    public int MentorId { get; set; }
    public Domain.Enums.PaymentStatus Status { get; set; }
}