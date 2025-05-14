using Domain.Enums;

namespace Domain.DTOs.Notification;

public class NotificationDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = null!;
    public string Message { get; set; } = null!;
    public NotificationType Type { get; set; }
    public int? RecipientId { get; set; }
    public string? RecipientName { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public bool IsSent { get; set; }
    public DateTime SentAt { get; set; }
    public bool SentByEmail { get; set; }
    public bool SentByTelegram { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
