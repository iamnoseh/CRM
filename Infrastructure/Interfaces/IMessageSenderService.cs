using Domain.DTOs.MessageSender;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IMessageSenderService
{
    Task<Response<GetMessageDto>> SendMessageAsync(SendMessageDto sendMessageDto);
    Task<Response<Domain.DTOs.OsonSms.OsonSmsSendResponseDto>> SendSmsToNumberAsync(string phoneNumber, string message);
    Task<Response<bool>> SendEmailToAddressAsync(string emailAddress, string subject, string messageContent, Microsoft.AspNetCore.Http.IFormFile? attachment);
}
