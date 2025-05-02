using Domain.DTOs.User;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IUserService
{
    // Базовые операции получения данных
    Task<PaginationResponse<List<GetUserDto>>> GetUsersAsync(UserFilter filter);
    Task<Response<GetUserDto>> GetUserByIdAsync(int id);
    Task<Response<GetUserDto>> GetCurrentUserAsync();
    
    // Поиск пользователей
    Task<Response<List<GetUserDto>>> SearchUsersAsync(string searchTerm);
    Task<Response<List<GetUserDto>>> GetUsersByRoleAsync(string role);
    
    // Статистика активности
    Task<Response<UserActivityDto>> GetUserActivityAsync(int userId);
}