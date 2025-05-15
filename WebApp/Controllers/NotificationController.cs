using Domain.DTOs.Notification;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[Route("api/[controller]")]
[ApiController]
public class NotificationController(INotificationService notificationService) : ControllerBase
{
    [HttpPost("emails")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> SendEmail([FromBody] EmailRequest request)
    {
        var result = await notificationService.SendEmailAsync(request.Email, request.Subject, request.Message);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("telegrams")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> SendTelegram([FromBody] TelegramRequest request)
    {
        var result = await notificationService.SendTelegramMessageAsync(request.ChatId, request.Message);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpPost("sms/{studentId}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> SendSms(int studentId, [FromBody] SmsRequest request)
    {
        var result = await notificationService.SendSmsAsync(studentId, request.Message);
        return StatusCode(result.StatusCode, result);
    }

    // Методы для отправки уведомлений студентам и группам
    [HttpPost("student/{studentId}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> NotifyStudent(int studentId, [FromBody] NotificationRequest request)
    {
        var result = await notificationService.SendStudentNotificationAsync(
            studentId, 
            request.Subject, 
            request.Message, 
            request.SendEmail, 
            request.SendTelegram);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("group/{groupId}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> NotifyGroup(int groupId, [FromBody] NotificationRequest request)
    {
        var result = await notificationService.SendGroupNotificationAsync(
            groupId, 
            request.Subject, 
            request.Message, 
            request.SendEmail, 
            request.SendTelegram);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("payment-reminder/{studentId}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> SendPaymentReminder(int studentId)
    {
        var result = await notificationService.SendPaymentReminderAsync(studentId);
        return StatusCode(result.StatusCode, result);
    }

    // История уведомлений
    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<List<NotificationDto>>>> GetAllNotifications()
    {
        var result = await notificationService.GetNotificationsAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "Admin,Teacher,Student")]
    public async Task<ActionResult<Response<List<NotificationDto>>>> GetStudentNotifications(int studentId)
    {
        var result = await notificationService.GetStudentNotificationsAsync(studentId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("type/{type}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<List<NotificationDto>>>> GetNotificationsByType(NotificationType type)
    {
        var result = await notificationService.GetNotificationsByTypeAsync(type);
        return StatusCode(result.StatusCode, result);
    }
}



