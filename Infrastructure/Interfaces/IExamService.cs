using Domain.DTOs.Exam;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

/// <summary>
/// Интерфейс сервиса для работы с экзаменами
/// </summary>
public interface IExamService
{
    // Методы для работы с экзаменами
    Task<Response<GetExamDto>> GetExamById(int id);
    Task<Response<List<GetExamDto>>> GetExams();
    Task<Response<List<GetExamDto>>> GetExamsByGroup(int groupId);
    Task<Response<string>> CreateExam(CreateExamDto examDto);
    Task<Response<string>> UpdateExam(int id, UpdateExamDto examDto);
    Task<Response<string>> DeleteExam(int id);
    
    // Методы для работы с оценками за экзамен
    Task<Response<GetExamGradeDto>> GetExamGradeById(int id);
    Task<Response<List<GetExamGradeDto>>> GetExamGradesByExam(int examId);
    Task<Response<List<GetExamGradeDto>>> GetExamGradesByStudent(int studentId);
    Task<Response<string>> CreateExamGrade(CreateExamGradeDto gradeDto);
    Task<Response<string>> UpdateExamGrade(int id, UpdateExamGradeDto gradeDto);
    Task<Response<string>> DeleteExamGrade(int id);
    Task<Response<string>> AddBonusPoint(int examGradeId, int bonusPoints);
}