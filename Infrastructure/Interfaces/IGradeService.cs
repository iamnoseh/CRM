using Domain.DTOs.Grade;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IGradeService
{
    // Общие методы для работы с оценками
    Task<Response<GetGradeDto>> GetGradeByIdAsync(int id);
    Task<Response<List<GetGradeDto>>> GetAllGradesAsync();
    Task<Response<string>> CreateGradeAsync(CreateGradeDto grade);
    Task<Response<string>> EditGradeAsync(UpdateGradeDto grade);
    Task<Response<string>> DeleteGradeAsync(int id);
    
    // Методы для получения оценок по различным параметрам
    Task<Response<List<GetGradeDto>>> GetGradesByStudentAsync(int studentId);
    Task<Response<List<GetGradeDto>>> GetGradesByGroupAsync(int groupId);
    Task<Response<List<GetGradeDto>>> GetGradesByLessonAsync(int lessonId);
    Task<Response<double>> GetStudentAverageGradeAsync(int studentId, int? groupId = null);
    
    // Методы для работы с оценками экзаменов
    Task<Response<List<GetGradeDto>>> GetGradesByExamAsync(int examId);
    Task<Response<string>> CreateExamGradeAsync(CreateGradeDto grade);
    Task<Response<double>> GetStudentExamAverageAsync(int studentId, int? groupId = null);
}