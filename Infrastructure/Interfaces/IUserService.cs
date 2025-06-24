using Domain.DTOs.User;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IUserService
{
    Task<PaginationResponse<List<GetUserDto>>> GetUsersAsync(UserFilter filter);
    Task<Response<GetUserDto>> GetUserByIdAsync(int id);
    Task<Response<GetUserDto>> GetCurrentUserAsync();

    Task<Response<List<GetUserDto>>> SearchUsersAsync(string searchTerm);
    Task<Response<List<GetUserDto>>> GetUsersByRoleAsync(string role);

    // Task<Response<UserActivityDto>> GetUserActivityAsync(int userId);

    Task<PaginationResponse<List<GetUserDto>>> GetUpcomingBirthdaysAsync(int page, int pageSize);
}