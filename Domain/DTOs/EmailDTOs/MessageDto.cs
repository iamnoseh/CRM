using MimeKit;

namespace Domain.DTOs.EmailDTOs;

public class EmailMessageDto
{
    public EmailMessageDto(IEnumerable<string> to, string subject, string content, List<string>? attachmentsPaths = null)
    {
        To = new List<MailboxAddress>();
        To.AddRange(to.Select(x => new MailboxAddress("email", x)));
        Subject = subject;
        Content = content;
        AttachmentsPaths = attachmentsPaths;
    }

    public List<MailboxAddress> To { get; set; }
    public string Subject { get; set; }
    public string Content { get; set; }
    public List<string>? AttachmentsPaths { get; set; }
}