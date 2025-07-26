using Domain.Enums;

namespace Domain.Entities;

public class Payment : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; }
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string? Description { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public int? CenterId { get; set; }
    public Center? Center { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}
