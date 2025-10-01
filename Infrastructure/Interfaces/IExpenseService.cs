using Domain.DTOs.Finance;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IExpenseService
{
    Task<Response<GetExpenseDto>> CreateAsync(CreateExpenseDto dto);
    Task<Response<GetExpenseDto>> UpdateAsync(int id, UpdateExpenseDto dto);
    Task<Response<bool>> DeleteAsync(int id);
    Task<Response<GetExpenseDto>> GetByIdAsync(int id);
    Task<Response<List<GetExpenseDto>>> GetAsync(ExpenseFilter filter);
}

