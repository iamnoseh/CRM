using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Finance;

public class WithdrawDto
{
<<<<<<< HEAD
    [Required(ErrorMessage = "Код кошелька обязателен")]
    public string AccountCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Сумма обязательна")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть больше нуля")]
=======
    [Required]
    public int StudentId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
>>>>>>> dd73a8b0538db2e63c27a6157bcdaa3e8bc0e8fa
    public decimal Amount { get; set; }

    public string? Reason { get; set; }
}

