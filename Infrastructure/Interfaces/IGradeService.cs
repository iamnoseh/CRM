using Domain.DTOs.Grade;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IGradeService
{
    Task<Response<GetGradeDto>> GetGradeByIdAsync(int id);
    Task<Response<List<GetGradeDto>>> GetAllGradesAsync();
    Task<Response<string>> CreateGradeAsync(CreateGradeDto grade);
    Task<Response<string>> EditGradeAsync(UpdateGradeDto grade);
    Task<Response<string>> DeleteGradeAsync(int id);
    Task<Response<List<GetGradeDto>>> GetGradesByStudentAsync(int studentId);
    Task<Response<List<GetGradeDto>>> GetGradesByGroupAsync(int groupId);
    Task<Response<List<GetGradeDto>>> GetGradesByLessonAsync(int lessonId);
    Task<Response<double>> GetStudentAverageGradeAsync(int studentId, int? groupId = null);
}