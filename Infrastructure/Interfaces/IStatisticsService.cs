using Domain.DTOs.Statistics;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IStatisticsService
{
    // Методы для базовой статистики студента
    Task<Response<StudentPerformanceDto>> GetStudentPerformanceAsync(int studentId, int groupId);
    
    // Базовая статистика группы
    Task<Response<GroupPerformanceDto>> GetGroupPerformanceAsync(int groupId);
    
    // Базовый ежемесячный отчет
    Task<Response<MonthlySummaryDto>> GetMonthlySummaryAsync(int centerId, int month, int year);
}
