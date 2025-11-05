using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Finance;

public class TopUpDto
{
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string AccountCode { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public string? Method { get; set; }
    public string? Notes { get; set; }
}


