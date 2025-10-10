using System.Net;
using Domain.DTOs.EmailDTOs;
using Domain.DTOs.MessageSender;
using Domain.Responses;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Domain.Enums;
using MimeKit.Text;
using Infrastructure.Services.EmailService;

namespace Infrastructure.Services;

public class MessageSenderService(
    IStudentService studentService,
    IEmailService emailService,
    IOsonSmsService osonSmsService,
    IWebHostEnvironment webHostEnvironment)
    : IMessageSenderService
{
    public async Task<Response<GetMessageDto>> SendMessageAsync(SendMessageDto sendMessageDto)
    {
        var attachmentPath = (string?)null;
        if (sendMessageDto.Attachment != null)
        {
            var uploadResult = await FileUploadHelper.UploadFileAsync(sendMessageDto.Attachment, 
                                                                     webHostEnvironment.WebRootPath, 
                                                                     "messages", 
                                                                     "document");
            if (uploadResult.StatusCode != (int)HttpStatusCode.OK)
            {
                return new Response<GetMessageDto>((HttpStatusCode)uploadResult.StatusCode, $"Хатогӣ ҳангоми боркунии замима: {uploadResult.Message}");
            }
            attachmentPath = uploadResult.Data;
        }

        foreach (var studentId in sendMessageDto.StudentIds)
        {
            var studentResponse = await studentService.GetStudentByIdAsync(studentId);
            if (studentResponse.StatusCode != (int)HttpStatusCode.OK || studentResponse.Data == null)
            {
                return new Response<GetMessageDto>(HttpStatusCode.NotFound, $"Донишҷӯ бо ID {studentId} ёфт нашуд.");
            }

            var student = studentResponse.Data;

            if (sendMessageDto.MessageType == MessageType.Email)
            {
                if (string.IsNullOrEmpty(student.Email))
                {
                    return new Response<GetMessageDto>(HttpStatusCode.BadRequest, $"Почтаи электронии донишҷӯ бо ID {studentId} мавҷуд нест.");
                }

                var emailMessage = sendMessageDto.MessageContent;
                List<string>? attachments = null;
                if (!string.IsNullOrEmpty(attachmentPath))
                {
                    attachments = new List<string> { Path.Combine(webHostEnvironment.WebRootPath, attachmentPath.TrimStart('/')) };
                }

                var emailDto = new EmailMessageDto(new[] { student.Email }, "Kavsar Academy", emailMessage, attachments);
                await emailService.SendEmail(emailDto, TextFormat.Html);
            }
            else if (sendMessageDto.MessageType == MessageType.SMS)
            {
                if (string.IsNullOrEmpty(student.Phone))
                {
                    return new Response<GetMessageDto>(HttpStatusCode.BadRequest, $"Рақами телефони донишҷӯ бо ID {studentId} мавҷуд нест.");
                }

                if (!string.IsNullOrEmpty(attachmentPath))
                {
                    // For SMS, we can't send attachments directly. We'll just send the text message.
                    // Or, we could shorten the URL and include it, but for simplicity, we'll omit attachments for SMS.
                }

                await osonSmsService.SendSmsAsync(student.Phone, sendMessageDto.MessageContent);
            }
        }

        return new Response<GetMessageDto>(new GetMessageDto
        {
            StudentIds = sendMessageDto.StudentIds,
            MessageContent = sendMessageDto.MessageContent,
            MessageType = sendMessageDto.MessageType,
            AttachmentPath = attachmentPath,
            SentAt = DateTime.UtcNow
        })
        { Message = "Паём бо муваффақият ирсол шуд." };
    }

    public async Task<Response<Domain.DTOs.OsonSms.OsonSmsSendResponseDto>> SendSmsToNumberAsync(string phoneNumber, string message)
    {
        return await osonSmsService.SendSmsAsync(phoneNumber, message);
    }

    public async Task<Response<bool>> SendEmailToAddressAsync(string emailAddress, string subject, string messageContent, Microsoft.AspNetCore.Http.IFormFile? attachment)
    {
        var attachmentPath = (string?)null;
        if (attachment != null)
        {
            var uploadResult = await FileUploadHelper.UploadFileAsync(attachment, 
                                                                     webHostEnvironment.WebRootPath, 
                                                                     "messages", 
                                                                     "document");
            if (uploadResult.StatusCode != (int)HttpStatusCode.OK)
            {
                return new Response<bool>(false, $"Хатогӣ ҳангоми боркунии замима: {uploadResult.Message}");
            }
            attachmentPath = uploadResult.Data;
        }

        List<string>? attachments = null;
        if (!string.IsNullOrEmpty(attachmentPath))
        {
            attachments = new List<string> { Path.Combine(webHostEnvironment.WebRootPath, attachmentPath.TrimStart('/')) };
        }

        var emailDto = new EmailMessageDto(new[] { emailAddress }, subject, messageContent, attachments);
        await emailService.SendEmail(emailDto, TextFormat.Html);
        return new Response<bool>(true) { Message = "Почтаи электронӣ бо муваффақият ирсол шуд." };
    }
}
