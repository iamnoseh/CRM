using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.MessageSender;

public class SendMessageDto
{
    public List<int> StudentIds { get; set; } = new List<int>();
    public string MessageContent { get; set; } = string.Empty;
    public MessageType MessageType { get; set; }
    public IFormFile? Attachment { get; set; }
}
