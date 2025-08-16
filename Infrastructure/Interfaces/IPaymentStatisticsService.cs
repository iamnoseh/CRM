using Domain.DTOs.Statistics;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IPaymentStatisticsService
{
    Task<Response<StudentPaymentStatisticsDto>> GetStudentPaymentStatisticsAsync(
        int studentId,
        int? groupId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);
    Task<Response<List<GroupPaymentStatisticsDto>>> GetDailyGroupPaymentStatisticsAsync(
        int groupId,
        DateTimeOffset date);
    
    Task<Response<GroupPaymentStatisticsDto>> GetGroupPaymentStatisticsAsync(
        int groupId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);
    
    Task<Response<GroupPaymentStatisticsDto>> GetMonthlyGroupPaymentStatisticsAsync(
        int groupId,
        int year,
        int month);
    
    Task<Response<List<CenterPaymentStatisticsDto>>> GetDailyCenterPaymentStatisticsAsync(
        int centerId,
        DateTimeOffset date);
    
    Task<Response<CenterPaymentStatisticsDto>> GetCenterPaymentStatisticsAsync(
        int centerId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);

    Task<Response<CenterPaymentStatisticsDto>> GetMonthlyCenterPaymentStatisticsAsync(
        int centerId,
        int year,
        int month);
}
