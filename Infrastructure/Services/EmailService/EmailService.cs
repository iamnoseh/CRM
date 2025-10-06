using Domain.DTOs.EmailDTOs;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Infrastructure.Services.EmailService
{
    public class EmailService(EmailConfiguration emailConfiguration, IConfiguration configuration) : IEmailService
    {

    #region SendEmail

    public async Task SendEmail(EmailMessageDto message, TextFormat format)
    {
        var emailMessage = CreateEmailMessage(message, format);
        await SendAsync(emailMessage);
    }

    #endregion

    #region CreateEmailMessage

    private MimeMessage CreateEmailMessage(EmailMessageDto message, TextFormat format)
    {
        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(configuration["EmailConfiguration:DisplayName"], emailConfiguration.From));
        emailMessage.To.AddRange(message.To);
        emailMessage.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = message.Content };

        if (message.AttachmentsPaths != null && message.AttachmentsPaths.Any())
        {
            foreach (var attachmentPath in message.AttachmentsPaths)
            {
                if (File.Exists(attachmentPath))
                {
                    bodyBuilder.Attachments.Add(attachmentPath);
                }
            }
        }

        emailMessage.Body = bodyBuilder.ToMessageBody();

        return emailMessage;
    }

    #endregion

    #region SendAsync

    private async Task SendAsync(MimeMessage mailMessage)
    {
        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(emailConfiguration.SmtpServer, emailConfiguration.Port, SecureSocketOptions.StartTls);
            client.AuthenticationMechanisms.Remove("OAUTH2");
            await client.AuthenticateAsync(emailConfiguration.Username, emailConfiguration.Password);
            await client.SendAsync(mailMessage);
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }

    #endregion

    }
}