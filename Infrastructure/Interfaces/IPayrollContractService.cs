using Domain.DTOs.Payroll;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IPayrollContractService
{
    Task<Response<GetPayrollContractDto>> CreateAsync(CreatePayrollContractDto dto);
    Task<Response<GetPayrollContractDto>> UpdateAsync(int id, UpdatePayrollContractDto dto);
    Task<Response<string>> DeactivateAsync(int id);
    Task<Response<GetPayrollContractDto>> GetByIdAsync(int id);
    Task<Response<GetPayrollContractDto>> GetActiveByMentorAsync(int mentorId);
    Task<Response<GetPayrollContractDto>> GetActiveByEmployeeAsync(int employeeUserId);
    Task<Response<List<GetPayrollContractDto>>> GetAllAsync();
    Task<PaginationResponse<List<GetPayrollContractDto>>> GetPaginatedAsync(PayrollContractFilter filter);
}
