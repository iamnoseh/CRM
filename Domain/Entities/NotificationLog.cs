using Domain.Enums;

namespace Domain.Entities;

public class NotificationLog : BaseEntity
{
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public int? StudentId { get; set; }
    public Student? Student { get; set; }
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
    public int? CenterId { get; set; }
    public Center? Center { get; set; }
    public bool SentByEmail { get; set; }
    public bool SentByTelegram { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentDateTime { get; set; } = DateTime.Now;
}
