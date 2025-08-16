using Domain.DTOs.Student;

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
    Task<PaginationResponse<List<GetStudentForSelectDto>>> GetStudentForSelect(StudentFilterForSelect filter);
    Task<Response<GetStudentDto>> GetStudentByIdAsync(int id);
    Task<Response<string>> UpdateUserProfileImageAsync(int studentId, IFormFile? profileImage);
    Task<Response<string>> UpdateStudentDocumentAsync(int studentId, IFormFile? documentFile);
    Task<PaginationResponse<List<GetStudentDto>>> GetStudentsPagination(StudentFilter filter);
    Task<Response<byte[]>> GetStudentDocument(int studentId);
    Task<Response<string>> UpdateStudentPaymentStatusAsync(UpdateStudentPaymentStatusDto dto);
    Task<PaginationResponse<List<GetSimpleDto>>>  GetSimpleStudents(StudentFilter filter);
}