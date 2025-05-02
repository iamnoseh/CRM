using Domain.DTOs.Notification;
using Domain.Enums;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface INotificationService
{
    // Базовые методы отправки
    Task<Response<string>> SendEmailAsync(string toEmail, string subject, string message);
    Task<Response<string>> SendTelegramMessageAsync(string chatId, string message);
    
    // Уведомления для студентов/групп
    Task<Response<string>> SendStudentNotificationAsync(int studentId, string subject, string message, bool sendEmail = true, bool sendTelegram = true);
    Task<Response<string>> SendGroupNotificationAsync(int groupId, string subject, string message, bool sendEmail = true, bool sendTelegram = true);
    
    // Базовые уведомления по событиям (только самые необходимые)
    Task<Response<string>> SendPaymentReminderAsync(int studentId);
    
    // История уведомлений
    Task<Response<List<NotificationDto>>> GetNotificationsAsync();
    Task<Response<List<NotificationDto>>> GetStudentNotificationsAsync(int studentId);
    Task<Response<List<NotificationDto>>> GetNotificationsByTypeAsync(NotificationType type);
    
}
