using Domain.DTOs.Statistics;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IPaymentStatisticsService
{
    // Омори пардохтҳои донишҷӯ
    Task<Response<StudentPaymentStatisticsDto>> GetStudentPaymentStatisticsAsync(
        int studentId,
        int? groupId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);

    // Омори рӯзонаи пардохтҳои гурӯҳ
    Task<Response<List<GroupPaymentStatisticsDto>>> GetDailyGroupPaymentStatisticsAsync(
        int groupId,
        DateTimeOffset date);

    // Омори пардохтҳои гурӯҳ
    Task<Response<GroupPaymentStatisticsDto>> GetGroupPaymentStatisticsAsync(
        int groupId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);

    // Омори моҳонаи пардохтҳои гурӯҳ
    Task<Response<GroupPaymentStatisticsDto>> GetMonthlyGroupPaymentStatisticsAsync(
        int groupId,
        int year,
        int month);

    // Омори рӯзонаи пардохтҳои марказ
    Task<Response<List<CenterPaymentStatisticsDto>>> GetDailyCenterPaymentStatisticsAsync(
        int centerId,
        DateTimeOffset date);

    // Омори пардохтҳои марказ
    Task<Response<CenterPaymentStatisticsDto>> GetCenterPaymentStatisticsAsync(
        int centerId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);

    // Омори моҳонаи пардохтҳои марказ
    Task<Response<CenterPaymentStatisticsDto>> GetMonthlyCenterPaymentStatisticsAsync(
        int centerId,
        int year,
        int month);
}
