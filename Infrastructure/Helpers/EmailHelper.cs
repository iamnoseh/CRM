using Domain.DTOs.EmailDTOs;
using Infrastructure.Services.EmailService;
using MimeKit.Text;

namespace Infrastructure.Helpers;

public static class EmailHelper
{
    public static async Task SendLoginDetailsEmailAsync(
        IEmailService emailService,
        string email,
        string username,
        string password,
        string entityType,
        string primaryColor,
        string accentColor)
    {
        try
        {
            string messageText = "Аккаунти шумо дар системаи мо сохта шуд. Барои Ворид ба система, аз чунин маълумоти воридшавӣ истифода кунед:";
            var emailContent = EmailTemplateHelperNew.GenerateLoginEmailTemplate(
                username, password, messageText, primaryColor, accentColor, entityType);

            var emailMessage = new EmailMessageDto(
                new List<string> { email },
                $"Your {entityType} Account Details",
                emailContent);

            await emailService.SendEmail(emailMessage, TextFormat.Html);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }
    }

    public static async Task SendResetPasswordCodeEmailAsync(
        IEmailService emailService,
        string email,
        string code)
    {
        try
        {
            string messageText = "Шумо дархости аз нав танзим кардани паролро кардаед. Ин аст коди шумо барои аз нав танзим кардан:";
            var emailContent = EmailTemplateHelperNew.GenerateResetCodeEmailTemplate(
                code, messageText, "#5E60CE", "#4EA8DE");

            var emailMessage = new EmailMessageDto(
                new List<string> { email },
                "Reset Password Code",
                emailContent);

            await emailService.SendEmail(emailMessage, TextFormat.Html);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send reset code email: {ex.Message}");
        }
    }
}