using System.Net;
using Domain.DTOs.EmailDTOs;
using Domain.DTOs.MessageSender;
using Domain.Responses;
using Infrastructure.Helpers;
using Infrastructure.Constants;
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
    #region SendMessageAsync

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
                return new Response<GetMessageDto>((HttpStatusCode)uploadResult.StatusCode, string.Format(Messages.File.UploadError, uploadResult.Message));
            }
            attachmentPath = uploadResult.Data;
        }

        foreach (var studentId in sendMessageDto.StudentIds)
        {
            var studentResponse = await studentService.GetStudentByIdAsync(studentId);
            if (studentResponse.StatusCode != (int)HttpStatusCode.OK || studentResponse.Data == null)
            {
                return new Response<GetMessageDto>(HttpStatusCode.NotFound, Messages.Student.NotFound);
            }

            var student = studentResponse.Data;

            if (sendMessageDto.MessageType == MessageType.Email)
            {
                if (string.IsNullOrEmpty(student.Email))
                {
                    return new Response<GetMessageDto>(HttpStatusCode.BadRequest, "Электронная почта студента отсутствует");
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
                    return new Response<GetMessageDto>(HttpStatusCode.BadRequest, "Номер телефона студента отсутствует");
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
        { Message = Messages.Common.Success };
    }

    #endregion

    #region SendSmsToNumberAsync

    public async Task<Response<Domain.DTOs.OsonSms.OsonSmsSendResponseDto>> SendSmsToNumberAsync(string phoneNumber, string message)
    {
        return await osonSmsService.SendSmsAsync(phoneNumber, message);
    }

    #endregion

    #region SendEmailToAddressAsync

    public async Task<Response<bool>> SendEmailToAddressAsync(SendEmailToAddressDto request)
    {
        var attachmentPath = (string?)null;
        if (request.Attachment != null)
        {
            var uploadResult = await FileUploadHelper.UploadFileAsync(request.Attachment,
                                                                     webHostEnvironment.WebRootPath,
                                                                     "messages",
                                                                     "document");
            if (uploadResult.StatusCode != (int)HttpStatusCode.OK)
            {
                return new Response<bool>(HttpStatusCode.BadRequest, string.Format(Messages.File.UploadError, uploadResult.Message));
            }
            attachmentPath = uploadResult.Data;
        }

        List<string>? attachments = null;
        if (!string.IsNullOrEmpty(attachmentPath))
        {
            attachments = new List<string> { Path.Combine(webHostEnvironment.WebRootPath, attachmentPath.TrimStart('/')) };
        }

        var emailDto = new EmailMessageDto(new[] { request.EmailAddress }, request.Subject, request.MessageContent, attachments);
        await emailService.SendEmail(emailDto, TextFormat.Html);
        return new Response<bool>(true) { Message = Messages.Common.Success };
    }

    #endregion
}
