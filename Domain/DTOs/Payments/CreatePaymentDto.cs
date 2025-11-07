using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.DTOs.Payments;

public class CreatePaymentDto
{
    [Required]
    public int StudentId { get; set; }
    [Required]
    public int GroupId { get; set; }
    [Required]
    [Range(1, 12)]
    public int Month { get; set; }
    [Required]
    [Range(2000, 3000)]
    public int Year { get; set; }
    [Range(1, 12)]
    public int? MonthsCount { get; set; }
    [Required]
    public PaymentMethod PaymentMethod { get; set; }
    [Range(0.01, double.MaxValue)]
    public decimal? Amount { get; set; }
    public string? TransactionId { get; set; }
    public string? Description { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Completed;
}


