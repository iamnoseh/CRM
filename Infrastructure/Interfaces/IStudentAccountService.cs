using Domain.DTOs.Finance;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IStudentAccountService
{
    Task<Response<GetStudentAccountDto>> GetByStudentIdAsync(int studentId);
    Task<Response<GetStudentAccountDto>> TopUpAsync(TopUpDto dto);
    Task<Response<int>> RunMonthlyChargeAsync(int month, int year);
    Task<Response<string>> ChargeForGroupAsync(int studentId, int groupId, int month, int year);
    Task<Response<List<GetAccountLogDto>>> GetLastLogsAsync(int studentId, int limit = 10);
    Task<PaginationResponse<List<AccountListItemDto>>> GetAccountsAsync(string? search, int pageNumber, int pageSize);
    Task<Response<MyWalletDto>> GetMyWalletAsync(int limit = 10);
}


