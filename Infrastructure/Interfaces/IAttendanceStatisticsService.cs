using Domain.DTOs.Statistics;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IAttendanceStatisticsService
{
    // Student statistics
    Task<Response<StudentAttendanceAllStatisticsDto>> GetStudentAttendanceStatisticsAsync(
        int studentId, 
        int? groupId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);

    // Group statistics
    Task<Response<GroupAttendanceAllStatisticsDto>> GetGroupAttendanceStatisticsAsync(
        int groupId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);

    // Daily group statistics
    Task<Response<List<GroupAttendanceAllStatisticsDto>>> GetDailyGroupAttendanceStatisticsAsync(
        int groupId,
        DateTimeOffset date);

    // Center statistics
    Task<Response<CenterAttendanceAllStatisticsDto>> GetCenterAttendanceStatisticsAsync(
        int centerId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);

    // Daily center statistics
    Task<Response<List<CenterAttendanceAllStatisticsDto>>> GetDailyCenterAttendanceStatisticsAsync(
        int centerId,
        DateTimeOffset date);
}
