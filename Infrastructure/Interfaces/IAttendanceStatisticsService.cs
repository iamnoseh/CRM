using Domain.DTOs.Statistics;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IAttendanceStatisticsService
{
    // Омори рӯзонаи иштирок
    Task<Response<DailyAttendanceSummaryDto>> GetDailyAttendanceSummaryAsync(DateTime date, int? centerId = null);
    
    // Рӯйхати донишҷӯёни ғоиб
    Task<Response<List<AbsentStudentDto>>> GetAbsentStudentsAsync(DateTime date, int? centerId = null);
    
    // Омори моҳонаи иштирок
    Task<Response<MonthlyAttendanceStatisticsDto>> GetMonthlyAttendanceStatisticsAsync(int month, int year, int? centerId = null);
    
    // Омори ҳафтаинаи иштирок
    Task<Response<List<DailyAttendanceSummaryDto>>> GetWeeklyAttendanceSummaryAsync(DateTime startDate, DateTime endDate, int? centerId = null);
    
    // Омори гурӯҳӣ барои рӯзи мушаххас
    Task<Response<List<StudentAttendanceStatisticsDto>>> GetGroupAttendanceForDateAsync(int groupId, DateTime date);
    
    // Донишҷӯёне ки вақти дарсиашон шудааст вале ғоибанд
    Task<Response<List<AbsentStudentDto>>> GetStudentsWithPaidLessonsButAbsentAsync(DateTime date, int? centerId = null);
    
    // Донишҷӯёне ки вақти дарсиашон шудааст ва ҳозиранд
    Task<Response<List<StudentAttendanceStatisticsDto>>> GetStudentsWithPaidLessonsAndPresentAsync(DateTime date, int? centerId = null);
}
