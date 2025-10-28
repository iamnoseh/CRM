using Domain.DTOs.User;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IUserService
{
    Task<PaginationResponse<List<GetUserDto>>> GetUsersAsync(UserFilter filter);
    Task<Response<GetUserDto>> GetUserByIdAsync(int id);
    Task<Response<GetUserDetailsDto>> GetCurrentUserAsync();

    Task<Response<List<GetUserDto>>> SearchUsersAsync(string searchTerm);
    Task<Response<List<GetUserDto>>> GetUsersByRoleAsync(string role);

    // Task<Response<UserActivityDto>> GetUserActivityAsync(int userId);

    Task<PaginationResponse<List<GetUserDto>>> GetUpcomingBirthdaysAsync(int page, int pageSize);

    Task<Response<string>> UpdateProfilePictureAsync(UpdateProfilePictureDto updateProfilePictureDto);
    Task<Response<string>> ChangeEmailAsync(int userId, string newEmail);
}