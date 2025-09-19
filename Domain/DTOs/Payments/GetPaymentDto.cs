using Domain.Enums;

namespace Domain.DTOs.Payments;

public class GetPaymentDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int? GroupId { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string? Description { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime PaymentDate { get; set; }
    public int? CenterId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}
