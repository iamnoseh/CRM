using Domain.DTOs.Classroom;
using Domain.DTOs.Schedule;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IClassroomService
{
    Task<Response<GetClassroomDto>> CreateClassroomAsync(CreateClassroomDto createDto);
    Task<PaginationResponse<List<GetClassroomDto>>> GetAllClassrooms(ClassroomFilter filter);
    Task<Response<GetClassroomDto>> GetClassroomByIdAsync(int id);
    Task<Response<List<GetClassroomDto>>> GetClassroomsByCenterAsync(int centerId);
    Task<Response<GetClassroomDto>> UpdateClassroomAsync(UpdateClassroomDto updateDto);
    Task<Response<bool>> DeleteClassroomAsync(int id);
    Task<Response<GetClassroomScheduleDto>> GetClassroomScheduleAsync(int classroomId, DateOnly? startDate = null, DateOnly? endDate = null);
    Task<Response<ScheduleConflictDto>> CheckScheduleConflictAsync(CreateScheduleDto scheduleDto);
    Task<Response<GetScheduleDto>> CreateScheduleAsync(CreateScheduleDto createDto);
    Task<Response<List<TimeSlotSuggestion>>> GetAvailableTimeSlotsAsync(int classroomId, DayOfWeek dayOfWeek, DateOnly date, TimeSpan duration);
    Task<Response<List<GetClassroomDto>>> GetAvailableClassroomsAsync(int centerId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, DateOnly date);
    Task<PaginationResponse<List<GetSimpleClassroomDto>>>  GetSimpleClassrooms(BaseFilter filter);
} 