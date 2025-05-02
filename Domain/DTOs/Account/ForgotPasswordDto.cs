using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Account;

public class ForgotPasswordDto
{
    [Required] [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public string Email { get; set; } = string.Empty;
}