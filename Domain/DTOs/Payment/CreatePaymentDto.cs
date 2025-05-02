using Domain.Enums;

namespace Domain.DTOs.Payment;

public class CreatePaymentDto
{
    public int StudentId { get; set; }
    public int? GroupId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string? Description { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Paid;
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public int? CenterId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}
