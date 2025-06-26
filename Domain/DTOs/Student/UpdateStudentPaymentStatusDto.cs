namespace Domain.DTOs.Student;

public class UpdateStudentPaymentStatusDto
{
    public int StudentId { get; set; }
    public Enums.PaymentStatus Status { get; set; }
}