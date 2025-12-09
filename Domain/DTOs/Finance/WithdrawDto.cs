using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Finance;

public class WithdrawDto
{
    [Required(ErrorMessage = "Код кошелька обязателен")]
    public string AccountCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Сумма обязательна")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть больше нуля")]
    public decimal Amount { get; set; }

    public string? Reason { get; set; }
}

