using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Account;

public class SendOtpDto
{
    [Required(ErrorMessage = "Номи корбар ҳатмист")]
    public string Username { get; set; } = string.Empty;
}

