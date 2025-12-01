namespace Domain.DTOs.Account;

public class VerifyOtpResponseDto
{
    public string ResetToken { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

