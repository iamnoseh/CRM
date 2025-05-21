using Domain.DTOs.Exam;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IExamService
{
    // Методы для работы с экзаменами
    Task<Response<GetExamDto>> GetExamById(int id);
    Task<Response<List<GetExamDto>>> GetExams();
    Task<Response<List<GetExamDto>>> GetExamsByGroup(int groupId);
    Task<Response<string>> CreateExam(CreateExamDto examDto);
    Task<Response<string>> UpdateExam(int id, UpdateExamDto examDto);
    Task<Response<string>> DeleteExam(int id);
}