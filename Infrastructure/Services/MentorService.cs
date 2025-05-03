using Domain.DTOs.Mentor;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Services.EmailService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Services;

public class MentorService(
    DataContext context,
    UserManager<User> userManager,
    string uploadPath,
    IEmailService emailService) : IMentorService
{
    public Task<Response<string>> CreateMentorAsync(CreateMentorDto createMentorDto)
    {
        throw new NotImplementedException();
    }

    public Task<Response<string>> UpdateMentorAsync(int id, UpdateMentorDto updateMentorDto)
    {
        throw new NotImplementedException();
    }

    public Task<Response<string>> DeleteMentorAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<GetMentorDto>>> GetMentors()
    {
        throw new NotImplementedException();
    }

    public Task<Response<GetMentorDto>> GetMentorByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Response<string>> UpdateUserProfileImageAsync(int Id, IFormFile? profileImage)
    {
        throw new NotImplementedException();
    }

    public Task<PaginationResponse<List<GetMentorDto>>> GetMentorsPagination(MentorFilter filter)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<GetMentorDto>>> GetMentorsByGroupAsync(int groupId)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<GetMentorDto>>> GetMentorsByCourseAsync(int courseId)
    {
        throw new NotImplementedException();
    }
}