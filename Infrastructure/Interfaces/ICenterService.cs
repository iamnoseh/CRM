using Domain.DTOs.Center;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface ICenterService
{
    Task<Response<string>> CreateCenterAsync(CreateCenterDto createCenterDto);
    Task<Response<string>> UpdateCenterAsync(int id, UpdateCenterDto updateCenterDto);
    Task<Response<CenterStatisticsDto>> GetCenterStatisticsAsync(int centerId);
    Task<Response<string>> DeleteCenterAsync(int id);
    Task<Response<List<GetCenterDto>>> GetCenters();
    Task<Response<GetCenterDto>> GetCenterByIdAsync(int id);
    Task<PaginationResponse<List<GetCenterDto>>> GetCentersPaginated(CenterFilter filter);
    Task<PaginationResponse<List<GetCenterSimpleDto>>> GetCentersSimplePaginated(int page, int pageSize);
    Task<Response<List<GetCenterGroupsDto>>> GetCenterGroupsAsync(int centerId);
    Task<Response<List<GetCenterStudentsDto>>> GetCenterStudentsAsync(int centerId);
    Task<Response<List<GetCenterMentorsDto>>> GetCenterMentorsAsync(int centerId);
    Task<Response<List<GetCenterCoursesDto>>> GetCenterCoursesAsync(int centerId);
    
    // Методы для расчета доходов центров
    Task<Response<string>> CalculateCenterIncomeAsync(int centerId);
    Task<Response<string>> CalculateAllCentersIncomeAsync();
}
