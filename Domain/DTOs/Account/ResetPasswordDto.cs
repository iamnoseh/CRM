namespace Domain.DTOs.Account;

public class ResetPasswordDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}