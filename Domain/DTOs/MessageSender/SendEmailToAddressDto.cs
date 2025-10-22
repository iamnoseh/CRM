using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.MessageSender;

public class SendEmailToAddressDto
{
    public string EmailAddress { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string MessageContent { get; set; } = null!;
    public IFormFile? Attachment { get; set; }
}
