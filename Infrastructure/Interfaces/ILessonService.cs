using Domain.DTOs.Lesson;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface ILessonService
{
    Task<Response<GetLessonDto>> CreateLessonAsync(CreateLessonDto createDto);
    Task<Response<GetLessonDto>> GetLessonByIdAsync(int id);
    Task<Response<List<GetLessonDto>>> GetLessonsByGroupAsync(int groupId);
    Task<Response<List<GetLessonDto>>> GetLessonsByClassroomAsync(int classroomId, DateOnly? startDate = null, DateOnly? endDate = null);
    Task<Response<List<GetLessonDto>>> GetLessonsByScheduleAsync(int scheduleId);
    Task<Response<GetLessonDto>> UpdateLessonAsync(UpdateLessonDto updateDto);
    Task<Response<bool>> DeleteLessonAsync(int id);
    Task<Response<List<GetLessonDto>>> GetWeeklyLessonsAsync(int groupId, DateOnly weekStart);
    Task<Response<bool>> CanScheduleLessonAsync(int classroomId, DateTimeOffset startTime, DateTimeOffset endTime);
} 