namespace Domain.DTOs.Student;

public class UpdateStudentPaymentStatusDto
{
    public int StudentId { get; set; }
    public Domain.Enums.PaymentStatus Status { get; set; }
}