using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Account;

public class ForgotPasswordDto
{
    [Required]
    public string Username { get; set; } = string.Empty;
}