using Domain.DTOs.Mentor;
using Domain.Filters;
using Domain.Responses;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Interfaces;

public interface IMentorService
{
    Task<Response<string>> CreateMentorAsync(CreateMentorDto createMentorDto);
    Task<Response<string>> UpdateMentorAsync(int id, UpdateMentorDto updateMentorDto);
    Task<Response<string>> DeleteMentorAsync(int id);
    Task<Response<List<GetMentorDto>>> GetMentors();
    Task<Response<GetMentorDto>> GetMentorByIdAsync(int id);
    Task<Response<string>> UpdateUserProfileImageAsync(int Id, IFormFile? profileImage);
    Task<Response<string>> UpdateMentorDocumentAsync(int mentorId, IFormFile? documentFile);
    Task<Response<byte[]>> GetMentorDocument(int mentorId);
    Task<PaginationResponse<List<GetMentorDto>>> GetMentorsPagination(MentorFilter filter);
    Task<Response<List<GetMentorDto>>> GetMentorsByGroupAsync(int groupId);
    Task<Response<List<GetMentorDto>>> GetMentorsByCourseAsync(int courseId);
    Task<Response<string>> UpdateMentorPaymentStatusAsync(int mentorId, Domain.Enums.PaymentStatus status);
}