using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Finance;

public class WithdrawDto
{
    [Required]
    public int StudentId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public string? Reason { get; set; }
}

