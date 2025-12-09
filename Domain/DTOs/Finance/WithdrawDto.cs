using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Finance;

public class WithdrawDto
{
    [Required]
    public string StudentAccount { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public string? Reason { get; set; }
}

