using Domain.DTOs.Schedule;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IScheduleService
{
    Task<Response<GetScheduleDto>> CreateScheduleAsync(CreateScheduleDto createDto);
    Task<Response<GetScheduleDto>> GetScheduleByIdAsync(int id);
    Task<Response<List<GetScheduleDto>>> GetSchedulesByClassroomAsync(int classroomId, DateOnly? startDate = null, DateOnly? endDate = null);
    Task<Response<List<GetScheduleDto>>> GetSchedulesByGroupAsync(int groupId);
    Task<Response<GetScheduleDto>> UpdateScheduleAsync(UpdateScheduleDto updateDto);
    Task<Response<bool>> DeleteScheduleAsync(int id);
    Task<Response<ScheduleConflictDto>> CheckScheduleConflictAsync(CreateScheduleDto scheduleDto);
    Task<Response<List<TimeSlotSuggestion>>> GetAvailableTimeSlotsAsync(int classroomId, DayOfWeek dayOfWeek, DateOnly date, TimeSpan duration);
    Task<Response<bool>> IsTimeSlotAvailableAsync(int classroomId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, DateOnly date);
    Task<Response<List<GetScheduleDto>>> GetWeeklyScheduleAsync(int centerId, DateOnly weekStart);
} 