using Domain.DTOs.User;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace Infrastructure.Services;

public class UserService(DataContext context, UserManager<User> userManager,
    IHttpContextAccessor httpContextAccessor) : IUserService
{
    #region GetUsersPagination
    
    public async Task<PaginationResponse<List<GetUserDto>>> GetUsersAsync(UserFilter filter)
    {
        try
        {
            var query = context.Users.Where(x => !x.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(filter.FullName))
            {
                query = query.Where(x => x.FullName.Contains(filter.FullName));
            }

            if (!string.IsNullOrEmpty(filter.Phone))
            {
                query = query.Where(x => x.PhoneNumber != null && x.PhoneNumber.Contains(filter.Phone));
            }

            if (filter.Role.HasValue)
            {
                string roleName = filter.Role.Value.ToString(); 

                query = query.Where(u => context.UserRoles
                    .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name })
                    .Any(ur => ur.UserId == u.Id && ur.RoleName == roleName));
            }

            var totalRecords = await query.CountAsync();
            var skip = (filter.PageNumber - 1) * filter.PageSize;
            var usersList = await query.OrderBy(x => x.Id)
                .Skip(skip)
                .Take(filter.PageSize)
                .ToListAsync();

            var userDtos = new List<GetUserDto>();
            foreach (var user in usersList)
            {
                var roles = await userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault(); 
                userDtos.Add(new GetUserDto
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email,
                    Address = user.Address,
                    Gender = user.Gender,
                    ActiveStatus = user.ActiveStatus,
                    Age = user.Age,
                    DateOfBirth = user.Birthday,
                    Image = user.ProfileImagePath,
                    Role = role,
                    CenterId = user.CenterId
                });
            }

            return new PaginationResponse<List<GetUserDto>>(
                userDtos, 
                filter.PageNumber, 
                filter.PageSize, 
                totalRecords);
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetUserDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetUserById
    
    public async Task<Response<GetUserDto>> GetUserByIdAsync(int id)
    {
        try
        {
            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (user == null)
            {
                return new Response<GetUserDto>(HttpStatusCode.NotFound, "User not found");
            }

            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            var dto = new GetUserDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                Address = user.Address,
                Gender = user.Gender,
                ActiveStatus = user.ActiveStatus,
                Age = user.Age,
                DateOfBirth = user.Birthday,
                Image = user.ProfileImagePath,
                Role = role,
                CenterId = user.CenterId
            };

            return new Response<GetUserDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetUserDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    
    #endregion

    #region GetCurrentUser
    
    public async Task<Response<GetUserDto>> GetCurrentUserAsync()
    {
        try
        {
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
            {
                return new Response<GetUserDto>(HttpStatusCode.Unauthorized, "User not authenticated");
            }
            
            return await GetUserByIdAsync(id);
        }
        catch (Exception ex)
        {
            return new Response<GetUserDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    
    #endregion
    
    #region SearchUsers
    
    public async Task<Response<List<GetUserDto>>> SearchUsersAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new Response<List<GetUserDto>>(HttpStatusCode.BadRequest, "Search term is required");
            }
            
            var users = await context.Users
                .Where(u => !u.IsDeleted && 
                           (u.FullName.Contains(searchTerm) || 
                            u.Email.Contains(searchTerm) || 
                            (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm))))
                .Take(20) // Ограничиваем количество результатов
                .ToListAsync();
                
            if (!users.Any())
            {
                return new Response<List<GetUserDto>>(HttpStatusCode.NotFound, "No users found matching the search criteria");
            }
            
            var userDtos = new List<GetUserDto>();
            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault();
                
                userDtos.Add(new GetUserDto
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email,
                    Address = user.Address,
                    Gender = user.Gender,
                    ActiveStatus = user.ActiveStatus,
                    Age = user.Age,
                    Image = user.ProfileImagePath,
                    Role = role,
                    CenterId = user.CenterId
                });
            }
            
            return new Response<List<GetUserDto>>(userDtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GetUserDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    
    #endregion
    
    #region GetUsersByRole
    
    public async Task<Response<List<GetUserDto>>> GetUsersByRoleAsync(string role)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return new Response<List<GetUserDto>>(HttpStatusCode.BadRequest, "Role is required");
            }
            
            var roleObj = await context.Roles.FirstOrDefaultAsync(r => r.Name == role);
            if (roleObj == null)
            {
                return new Response<List<GetUserDto>>(HttpStatusCode.NotFound, $"Role '{role}' not found");
            }
            
            var userIds = await context.UserRoles
                .Where(ur => ur.RoleId == roleObj.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();
                
            var users = await context.Users
                .Where(u => !u.IsDeleted && userIds.Contains(u.Id))
                .ToListAsync();
                
            if (!users.Any())
            {
                return new Response<List<GetUserDto>>(HttpStatusCode.NotFound, $"No users found with role '{role}'");
            }
            
            var userDtos = users.Select(user => new GetUserDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                Address = user.Address,
                Gender = user.Gender,
                ActiveStatus = user.ActiveStatus,
                Age = user.Age,
                DateOfBirth = user.Birthday,
                Image = user.ProfileImagePath,
                Role = role,
                CenterId = user.CenterId
            }).ToList();
            
            return new Response<List<GetUserDto>>(userDtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GetUserDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    
    #endregion
    
    #region UpcomingBirthdays
    public async Task<PaginationResponse<List<GetUserDto>>> GetUpcomingBirthdaysAsync(int page, int pageSize)
    {
        var today = DateTime.Today;
        var end = today.AddDays(7);
        var users = await context.Users
            .Where(u => !u.IsDeleted &&
                (
                    (u.Birthday.Month == today.Month && u.Birthday.Day >= today.Day) ||
                    (u.Birthday.Month == end.Month && u.Birthday.Day <= end.Day) ||
                    (u.Birthday.Month > today.Month && u.Birthday.Month < end.Month)
                )
            )
            .OrderBy(u => u.Birthday.Month)
            .ThenBy(u => u.Birthday.Day)
            .ToListAsync();

        // Filter to only those within the next 7 days (handles year wrap)
        var filtered = users.Where(u =>
        {
            var nextBirthday = new DateTime(today.Year, u.Birthday.Month, u.Birthday.Day);
            if (nextBirthday < today)
                nextBirthday = nextBirthday.AddYears(1);
            var diff = (nextBirthday - today).TotalDays;
            return diff >= 0 && diff <= 7;
        }).ToList();

        var total = filtered.Count;
        var paged = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var result = new List<GetUserDto>();
        foreach (var user in paged)
        {
            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();
            result.Add(new GetUserDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                Address = user.Address,
                Gender = user.Gender,
                ActiveStatus = user.ActiveStatus,
                Age = user.Age,
                DateOfBirth = user.Birthday,
                Image = user.ProfileImagePath,
                Role = role,
                CenterId = user.CenterId
            });
        }
        return new PaginationResponse<List<GetUserDto>>(result, page, pageSize, total);
    }
    #endregion
}
