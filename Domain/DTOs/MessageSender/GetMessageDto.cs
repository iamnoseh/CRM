using Domain.Enums;

namespace Domain.DTOs.MessageSender;

public class GetMessageDto
{
    public int Id { get; set; }
    public List<int> StudentIds { get; set; } = new List<int>();
    public string MessageContent { get; set; } = string.Empty;
    public MessageType MessageType { get; set; }
    public string? AttachmentPath { get; set; }
    public DateTime SentAt { get; set; }
}
