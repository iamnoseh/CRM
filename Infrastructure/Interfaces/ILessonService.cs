using Domain.DTOs.Lesson;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface ILessonService
{
    Task<Response<List<GetLessonDto>>> GetLessons();
    Task<Response<GetLessonDto>> GetLessonById(int id);
    Task<Response<string>> CreateLesson(CreateLessonDto createLessonDto);
    Task<Response<string>> UpdateLesson(UpdateLessonDto updateLessonDto);
    Task<Response<string>> DeleteLesson(int id);
    Task<PaginationResponse<List<GetLessonDto>>> GetLessonsPaginated(BaseFilter filter);
    Task<Response<List<GetLessonDto>>> GetLessonsByGroup(int groupId);
    Task<Response<string>> CreateWeeklyLessons(int groupId, int weekIndex, DateTimeOffset startDate);
}