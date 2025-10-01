using Domain.DTOs.Course;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface ICourseService
{
    Task<Response<string>> CreateCourseAsync(CreateCourseDto createCourseDto);
    Task<Response<string>> UpdateCourseAsync(UpdateCourseDto updateCourseDto);
    Task<Response<string>> DeleteCourseAsync(int id);
    Task<Response<List<GetCourseDto>>> GetCourses();
    Task<Response<GetCourseDto>> GetCourseByIdAsync(int id);
    Task<PaginationResponse<List<GetCourseDto>>> GetCoursesPagination(CourseFilter filter);
    Task<Response<List<GetCourseDto>>> GetCoursesByMentorAsync(int mentorId);
    Task<Response<GetCourseGroupsDto>> GetCourseGroupsAndCountAsync(int courseId);
    Task<PaginationResponse<List<GetSimpleCourseDto>>>  GetSimpleCourses(BaseFilter  filter);
}