using Domain.DTOs.Payroll;
using Domain.Enums;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IPayrollService
{
    Task<Response<GetWorkLogDto>> CreateWorkLogAsync(CreateWorkLogDto dto);
    Task<Response<string>> DeleteWorkLogAsync(int id);
    Task<Response<List<GetWorkLogDto>>> GetWorkLogsAsync(int? mentorId, int? employeeUserId, int month, int year);
    Task<Response<decimal>> GetTotalHoursAsync(int? mentorId, int? employeeUserId, int month, int year);

    Task<Response<GetPayrollRecordDto>> CalculatePayrollAsync(CalculatePayrollDto dto);
    Task<Response<List<GetPayrollRecordDto>>> CalculateAllForMonthAsync(int month, int year);
    Task<Response<GetPayrollRecordDto>> AddBonusFineAsync(AddBonusFineDto dto);
    Task<Response<GetPayrollRecordDto>> ApproveAsync(int payrollRecordId);
    Task<Response<GetPayrollRecordDto>> MarkAsPaidAsync(int payrollRecordId, MarkAsPaidDto dto);
    Task<Response<GetPayrollRecordDto>> GetPayrollRecordByIdAsync(int id);
    Task<Response<List<GetPayrollRecordDto>>> GetPayrollRecordsAsync(int month, int year);
    Task<PaginationResponse<List<GetPayrollRecordDto>>> GetPayrollRecordsPaginatedAsync(PayrollFilter filter);

    Task<Response<GetAdvanceDto>> CreateAdvanceAsync(CreateAdvanceDto dto);
    Task<Response<string>> CancelAdvanceAsync(int id);
    Task<Response<List<GetAdvanceDto>>> GetAdvancesAsync(int? mentorId, int? employeeUserId, int? month, int? year);
    Task<Response<decimal>> GetPendingAdvancesAmountAsync(int? mentorId, int? employeeUserId, int month, int year);

    Task<Response<List<PaymentHistoryDto>>> GetPaymentHistoryAsync(int? mentorId, int? employeeUserId, int? month, int? year);

    Task<Response<PayrollSummaryDto>> GetMonthlySummaryAsync(int month, int year);
}
