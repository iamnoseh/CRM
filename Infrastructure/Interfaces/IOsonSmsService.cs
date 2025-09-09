using Domain.DTOs.OsonSms;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IOsonSmsService
{
    Task<Response<OsonSmsSendResponseDto>> SendSmsAsync(string phoneNumber, string message);
    Task<Response<OsonSmsStatusResponseDto>> CheckSmsStatusAsync(string msgId);
    Task<Response<OsonSmsBalanceResponseDto>> CheckBalanceAsync();
}
