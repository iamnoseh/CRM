using Domain.DTOs.MessageSender;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IMessageSenderService
{
    Task<Response<GetMessageDto>> SendMessageAsync(SendMessageDto sendMessageDto);
}
