using Domain.DTOs.Exam;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IExamService
{
    Task<Response<string>> CreateExam(CreateExamDto request);
    Task<Response<string>> UpdateExam(int id, UpdateExamDto request);
    Task<Response<string>> DeleteExam(int id);
    Task<Response<List<GetExamDto>>> GetExams();
    Task<Response<GetExamDto>> GetExamById(int id);
    Task<Response<List<GetExamDto>>> GetExamsByStudent(int studentId);
    Task<Response<List<GetExamDto>>> GetExamsByGroup(int groupId);
    Task<Response<double>> GetStudentExamAverageScore(int studentId, int groupId);
}