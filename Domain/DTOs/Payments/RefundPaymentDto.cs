using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Payments;

public class RefundPaymentDto
{
    [Required]
    public decimal Amount { get; set; }

    public string? Reason { get; set; }
}


