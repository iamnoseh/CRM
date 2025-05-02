using Domain.DTOs.Payment;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IPaymentService
{
    Task<Response<string>> CreatePaymentAsync(CreatePaymentDto createPaymentDto);
    Task<Response<string>> UpdatePaymentAsync(int id, CreatePaymentDto updatePaymentDto);
    Task<Response<string>> DeletePaymentAsync(int id);
    Task<Response<List<GetPaymentDto>>> GetAllPaymentsAsync();
    Task<Response<GetPaymentDto>> GetPaymentByIdAsync(int id);
    Task<Response<List<GetPaymentDto>>> GetPaymentsByStudentAsync(int studentId);
    Task<Response<List<GetPaymentDto>>> GetPaymentsByGroupAsync(int groupId);
    Task<Response<List<GetPaymentDto>>> GetPaymentsByCenterAsync(int centerId);
    Task<PaginationResponse<List<GetPaymentDto>>> GetPaymentsPaginationAsync(BaseFilter filter);
    Task<Response<decimal>> GetTotalPaymentAmountByStudentAsync(int studentId);
    
    Task<Response<string>> MarkPaymentAsCompletedAsync(int paymentId);
    Task<Response<string>> MarkPaymentAsFailedAsync(int paymentId);
}
