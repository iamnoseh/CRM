namespace Domain.DTOs.Notification;

/// <summary>
/// DTO барои фиристодани почтаи электронӣ
/// </summary>
public class EmailRequest
{
    public string Email { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Message { get; set; } = null!;
}

/// <summary>
/// DTO барои фиристодани паёми Telegram
/// </summary>
public class TelegramRequest
{
    public string ChatId { get; set; } = null!;
    public string Message { get; set; } = null!;
}

/// <summary>
/// DTO барои огоҳиномаҳои умумӣ
/// </summary>
public class NotificationRequest
{
    public string Subject { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool SendEmail { get; set; } = true;
    public bool SendTelegram { get; set; } = true;
}

/// <summary>
/// DTO барои фиристодани SMS-паёмак
/// </summary>
public class SmsRequest
{
    public string Message { get; set; } = null!;
}
