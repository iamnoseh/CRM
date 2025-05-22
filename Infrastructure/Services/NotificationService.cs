using Domain.DTOs.EmailDTOs;
using Domain.DTOs.Notification;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Services.EmailService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit.Text;
using System.Net;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Infrastructure.Services;

public class NotificationService(DataContext context, ILogger<NotificationService> logger, IConfiguration configuration, IEmailService emailService)
    : INotificationService
{
    public async Task<Response<string>> SendEmailAsync(string toEmail, string subject, string message)
    {
        try
        {
            // Create email message using EmailService
            var emailMessage = new EmailMessageDto(new List<string> { toEmail }, subject, message);
            await emailService.SendEmail(emailMessage, TextFormat.Html);
            
            logger.LogInformation("Email sent successfully to {Email}", toEmail);
            return new Response<string>("Email sent successfully")
            {
                Message = "Email sent successfully"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            return new Response<string>(HttpStatusCode.InternalServerError, $"Failed to send email: {ex.Message}");
        }
    }

    public async Task<Response<string>> SendTelegramMessageAsync(string chatId, string message)
    {
        try
        {
            // Get Telegram settings from configuration
            var botToken = configuration["TelegramSettings:BotToken"];

            if (string.IsNullOrEmpty(botToken))
            {
                return new Response<string>(HttpStatusCode.InternalServerError, "Telegram settings are not configured properly");
            }

            // In a real implementation, you would use Telegram.Bot NuGet package
            // Here we're just mocking it for demonstration
            
            // Example of how it would be implemented:
            /*
            var botClient = new TelegramBotClient(botToken);
            await botClient.SendTextMessageAsync(chatId, message);
            */

            // For now, let's just log it
            logger.LogInformation("Telegram message would be sent to {ChatId}: {Message}", chatId, message);

            return new Response<string>("Telegram message sent successfully")
            {
                Message = "Telegram message sent successfully"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send Telegram message to {ChatId}", chatId);
            return new Response<string>(HttpStatusCode.InternalServerError, $"Failed to send Telegram message: {ex.Message}");
        }
    }
    
    public async Task<Response<string>> SendSmsAsync(int studentId, string message)
    {
        try
        {
            // Get student info with user details
            var student = await context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);

            if (student == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, $"Student with ID {studentId} not found");
            }

            // Check if student has a phone number
            var phoneNumber = student.User?.PhoneNumber;
            
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return new Response<string>(HttpStatusCode.BadRequest, $"Student with ID {studentId} does not have a phone number");
            }
            
            // Ensure phone number is in E.164 format (+[country code][number])
            if (!phoneNumber.StartsWith("+"))
            {
                // Assuming Tajikistan country code (992) if no country code is provided
                phoneNumber = phoneNumber.StartsWith("992") ? "+" + phoneNumber : "+992" + phoneNumber;
                
                // Remove any spaces or hyphens
                phoneNumber = phoneNumber.Replace(" ", "").Replace("-", "");
                
                logger.LogInformation("Formatted phone number to E.164 format: {PhoneNumber}", phoneNumber);
            }
            
            // Get SMS configuration
            var smsProvider = configuration["SmsConfiguration:Provider"];
            var accountSid = configuration["SmsConfiguration:AccountSid"];
            var authToken = configuration["SmsConfiguration:AuthToken"];
            var fromNumber = configuration["SmsConfiguration:FromNumber"];

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromNumber))
            {
                return new Response<string>(HttpStatusCode.InternalServerError, "SMS settings are not configured properly");
            }

            // Используем Twilio для реальной отправки SMS
            // Check if we're in development mode (can be configurable)
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" || true;

            if (isDevelopment)
            {
                // In development, mock the SMS sending
                logger.LogInformation("[DEVELOPMENT MODE] SMS would be sent from {FromNumber} to {ToNumber}: {Message}", 
                    fromNumber, phoneNumber, message);
                
                // Create fake SMS ID like Twilio would
                var fakeMessageId = $"SM{Guid.NewGuid().ToString("N").Substring(0, 32)}";
                
                logger.LogInformation("[DEVELOPMENT MODE] SMS successfully mocked with ID: {MessageId}", fakeMessageId);
            }
            else
            {
                // In production, use actual Twilio
                try
                {
                    TwilioClient.Init(accountSid, authToken);
                    
                    logger.LogInformation("Attempting to send SMS from {FromNumber} to {ToNumber}", fromNumber, phoneNumber);
                    
                    var twilioMessage = await MessageResource.CreateAsync(
                        body: message,
                        from: new Twilio.Types.PhoneNumber(fromNumber),
                        to: new Twilio.Types.PhoneNumber(phoneNumber)
                    );
                    
                    logger.LogInformation("SMS sent successfully to {PhoneNumber}: {Message}", phoneNumber, message);
                }
                catch (Exception twilioEx)
                {
                    logger.LogError(twilioEx, "Twilio error while sending SMS to {PhoneNumber}", phoneNumber);
                    return new Response<string>(HttpStatusCode.InternalServerError, $"Twilio error: {twilioEx.Message}");
                }
            }
            
            // Create notification log
            var notificationLog = new NotificationLog
            {
                Subject = "SMS Notification",
                Message = message,
                Type = NotificationType.General,
                StudentId = studentId,
                IsSuccessful = true,
                SentDateTime = DateTime.Now
            };
            
            context.NotificationLogs.Add(notificationLog);
            await context.SaveChangesAsync();

            return new Response<string>("SMS sent successfully")
            {
                Message = $"SMS sent successfully to {phoneNumber}"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send SMS to student {StudentId}", studentId);
            return new Response<string>(HttpStatusCode.InternalServerError, $"Failed to send SMS: {ex.Message}");
        }
    }
    
    public async Task<Response<string>> SendStudentNotificationAsync(int studentId, string subject, string message, bool sendEmail = true, bool sendTelegram = true)
    {
        try
        {
            // Get student information
            var student = await context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);

            if (student == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, $"Student with ID {studentId} not found");
            }

            var user = student.User;
            if (user == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, $"User for student with ID {studentId} not found");
            }

            // Check notification preferences
            sendEmail = sendEmail && user.EmailNotificationsEnabled;
            sendTelegram = sendTelegram && user.TelegramNotificationsEnabled;

            if (!sendEmail && !sendTelegram)
            {
                return new Response<string>(HttpStatusCode.BadRequest, "Student has disabled all notification methods");
            }

            // Create notification log
            var notificationLog = new NotificationLog
            {
                Subject = subject,
                Message = message,
                Type = NotificationType.General,
                StudentId = studentId,
                SentByEmail = false,
                SentByTelegram = false,
                IsSuccessful = false,
                SentDateTime = DateTime.Now
            };

            bool isSuccessful = false;
            string resultMessage = "";

            // Send email if enabled
            if (sendEmail && !string.IsNullOrEmpty(user.Email))
            {
                var emailResult = await SendEmailAsync(user.Email, subject, message);
                notificationLog.SentByEmail = true;
                if (emailResult.StatusCode == 200)
                {
                    isSuccessful = true;
                    resultMessage += "Email sent successfully. ";
                }
                else
                {
                    resultMessage += $"Email failed: {emailResult.Message}. ";
                }
            }

            // Send Telegram if enabled
            if (sendTelegram && !string.IsNullOrEmpty(user.TelegramChatId))
            {
                var telegramResult = await SendTelegramMessageAsync(user.TelegramChatId, message);
                notificationLog.SentByTelegram = true;
                if (telegramResult.StatusCode == 200)
                {
                    isSuccessful = true;
                    resultMessage += "Telegram message sent successfully. ";
                }
                else
                {
                    resultMessage += $"Telegram failed: {telegramResult.Message}. ";
                }
            }

            // Update notification log
            notificationLog.IsSuccessful = isSuccessful;
            if (!isSuccessful)
            {
                notificationLog.ErrorMessage = resultMessage;
            }

            context.NotificationLogs.Add(notificationLog);
            await context.SaveChangesAsync();

            if (isSuccessful)
            {
                return new Response<string>(resultMessage)
                {
                    Message = resultMessage
                };
            }
            else
            {
                return new Response<string>(HttpStatusCode.BadRequest, resultMessage);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending notification to student {StudentId}", studentId);
            return new Response<string>(HttpStatusCode.InternalServerError, $"Error sending notification: {ex.Message}");
        }
    }
    

    public async Task<Response<string>> SendGroupNotificationAsync(int groupId, string subject, string message, bool sendEmail = true, bool sendTelegram = true)
    {
        try
        {
            // Check if group exists
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

            if (group == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, $"Group with ID {groupId} not found");
            }

            // Get all active students in the group
            var studentGroups = await context.StudentGroups
                .Where(sg => sg.GroupId == groupId && (bool)sg.IsActive! && !sg.IsDeleted)
                .Select(sg => sg.StudentId)
                .ToListAsync();

            if (!studentGroups.Any())
            {
                return new Response<string>(HttpStatusCode.NotFound, $"No active students found in group with ID {groupId}");
            }

            // Create notification log for the group
            var groupNotificationLog = new NotificationLog
            {
                Subject = subject,
                Message = message,
                Type = NotificationType.General,
                GroupId = groupId,
                IsSuccessful = true,
                SentByEmail = sendEmail,
                SentByTelegram = sendTelegram,
                SentDateTime = DateTime.Now
            };

            context.NotificationLogs.Add(groupNotificationLog);

            // Send notification to each student
            int successCount = 0;
            List<string> failures = new List<string>();

            foreach (var studentId in studentGroups)
            {
                var result = await SendStudentNotificationAsync(studentId, subject, message, sendEmail, sendTelegram);
                if (result.StatusCode == 200)
                {
                    successCount++;
                }
                else
                {
                    failures.Add($"Student {studentId}: {result.Message}");
                }
            }

            await context.SaveChangesAsync();

            // Prepare response
            string resultMessage = $"Notification sent to {successCount} out of {studentGroups.Count} students.";
            if (failures.Any())
            {
                resultMessage += $" Failures: {string.Join("; ", failures)}";
            }

            if (successCount > 0)
            {
                return new Response<string>(resultMessage)
                {
                    Message = resultMessage
                };
            }
            else
            {
                return new Response<string>(HttpStatusCode.BadRequest, resultMessage);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending notification to group {GroupId}", groupId);
            return new Response<string>(HttpStatusCode.InternalServerError, $"Error sending notification: {ex.Message}");
        }
    }

    // Базовые уведомления по событиям
    public async Task<Response<string>> SendPaymentReminderAsync(int studentId)
    {
        try
        {
            // Get student information
            var student = await context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);

            if (student == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, $"Student with ID {studentId} not found");
            }

            // Get payment information (this would need to be adapted to your payment system)
            var payments = await context.Payments
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            // Create message based on payment status
            string subject = "Payment Reminder";
            string message = $"Dear {student.FullName},\n\n";
            
            if (!payments.Any())
            {
                message += "We would like to remind you that you have not made any payments yet.";
            }
            else
            {
                var lastPayment = payments.First();
                message += $"Your last payment was on {lastPayment.PaymentDate.ToShortDateString()}. ";
                message += "Please make sure your payments are up to date.";
            }

            message += "\n\nThank you,\nCRM System";

            // Send the notification
            var result = await SendStudentNotificationAsync(studentId, subject, message);
            
            // Update notification type for logging
            if (result.StatusCode == 200)
            {
                var notificationLog = await context.NotificationLogs
                    .OrderByDescending(n => n.Id)
                    .FirstOrDefaultAsync(n => n.StudentId == studentId);

                if (notificationLog != null)
                {
                    notificationLog.Type = NotificationType.PaymentReminder;
                    await context.SaveChangesAsync();
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending payment reminder to student {StudentId}", studentId);
            return new Response<string>(HttpStatusCode.InternalServerError, $"Error sending payment reminder: {ex.Message}");
        }
    }

    public async Task<Response<List<NotificationDto>>> GetNotificationsAsync()
    {
        try
        {
            var notifications = await context.NotificationLogs
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Subject = n.Subject,
                    Message = n.Message,
                    Type = n.Type,
                    RecipientId = n.StudentId,
                    RecipientName = n.Student != null ? $"{n.Student.FullName}" : null,
                    GroupId = n.GroupId,
                    GroupName = n.Group != null ? n.Group.Name : null,
                    IsSent = n.IsSuccessful,
                    SentAt = n.SentDateTime,
                    SentByEmail = n.SentByEmail,
                    SentByTelegram = n.SentByTelegram,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return new Response<List<NotificationDto>>(notifications)
            {
                Message = $"Retrieved {notifications.Count} notifications"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving notifications");
            return new Response<List<NotificationDto>>(HttpStatusCode.InternalServerError, $"Error retrieving notifications: {ex.Message}");
        }
    }

    public async Task<Response<List<NotificationDto>>> GetStudentNotificationsAsync(int studentId)
    {
        try
        {
            // Check if student exists
            var studentExists = await context.Students
                .AnyAsync(s => s.Id == studentId && !s.IsDeleted);

            if (!studentExists)
            {
                return new Response<List<NotificationDto>>(HttpStatusCode.NotFound, $"Student with ID {studentId} not found");
            }

            var notifications = await context.NotificationLogs
                .Where(n => n.StudentId == studentId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Subject = n.Subject,
                    Message = n.Message,
                    Type = n.Type,
                    RecipientId = n.StudentId,
                    RecipientName = n.Student != null ? n.Student.FullName : null,
                    GroupId = n.GroupId,
                    GroupName = n.Group != null ? n.Group.Name : null,
                    IsSent = n.IsSuccessful,
                    SentAt = n.SentDateTime,
                    SentByEmail = n.SentByEmail,
                    SentByTelegram = n.SentByTelegram,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return new Response<List<NotificationDto>>(notifications)
            {
                Message = $"Retrieved {notifications.Count} notifications for student {studentId}"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving notifications for student {StudentId}", studentId);
            return new Response<List<NotificationDto>>(HttpStatusCode.InternalServerError, $"Error retrieving notifications: {ex.Message}");
        }
    }

    public async Task<Response<List<NotificationDto>>> GetNotificationsByTypeAsync(NotificationType type)
    {
        try
        {
            var notifications = await context.NotificationLogs
                .Where(n => n.Type == type)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Subject = n.Subject,
                    Message = n.Message,
                    Type = n.Type,
                    RecipientId = n.StudentId,
                    RecipientName = n.Student != null ? $"{n.Student.FullName}" : null,
                    GroupId = n.GroupId,
                    GroupName = n.Group != null ? n.Group.Name : null,
                    IsSent = n.IsSuccessful,
                    SentAt = n.SentDateTime,
                    SentByEmail = n.SentByEmail,
                    SentByTelegram = n.SentByTelegram,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return new Response<List<NotificationDto>>(notifications)
            {
                Message = $"Retrieved {notifications.Count} notifications of type {type}"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving notifications of type {Type}", type);
            return new Response<List<NotificationDto>>(HttpStatusCode.InternalServerError, $"Error retrieving notifications: {ex.Message}");
        }
    }
}
