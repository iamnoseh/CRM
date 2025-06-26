using Domain.DTOs.Student;
using Domain.Enums;
using Domain.Filters;
using Domain.Responses;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Interfaces;

public interface IStudentService
{
    Task<Response<string>> CreateStudentAsync(CreateStudentDto createStudentDto);
    Task<Response<string>> UpdateStudentAsync(int id, UpdateStudentDto updateStudentDto);
    Task<Response<string>> DeleteStudentAsync(int id);
    Task<Response<List<GetStudentDto>>> GetStudents();
    Task<Response<GetStudentDto>> GetStudentByIdAsync(int id);
    Task<Response<string>> UpdateUserProfileImageAsync(int studentId, IFormFile? profileImage);
    Task<Response<string>> UpdateStudentDocumentAsync(int studentId, IFormFile? documentFile);
    Task<PaginationResponse<List<GetStudentDto>>> GetStudentsPagination(StudentFilter filter);
    Task<Response<byte[]>> GetStudentDocument(int studentId);
    Task<Response<GetStudentDetailedDto>> GetStudentDetailedAsync(int id);
    //Average
    Task<Response<GetStudentAverageDto>> GetStudentAverageAsync(int studentId,int groupId);
    Task<Response<string>> UpdateStudentPaymentStatusAsync(UpdateStudentPaymentStatusDto dto);
}