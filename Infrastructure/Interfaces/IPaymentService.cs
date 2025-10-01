using Domain.DTOs.Payments;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IPaymentService
{
    Task<Response<GetPaymentDto>> CreateAsync(CreatePaymentDto dto);
    Task<Response<GetPaymentDto>> GetByIdAsync(int id);
}
