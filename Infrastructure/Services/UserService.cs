// using Domain.DTOs.User;
// using Domain.Entities;
// using Domain.Filters;
// using Domain.Responses;
// using Infrastructure.Data;
// using Infrastructure.Interfaces;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.EntityFrameworkCore;
// using System.Security.Claims;
//
// namespace Infrastructure.Services;
//
// public class UserService(DataContext context, UserManager<User> userManager, IHttpContextAccessor httpContextAccessor) : IUserService
// {
//     #region GetUsersPagination
//     
//     public async Task<PaginationResponse<List<GetUserDto>>> GetUsersAsync(UserFilter filter)
//     {
//         var query = context.Users.Where(x => !x.IsDeleted).AsQueryable();
//
//         if (!string.IsNullOrEmpty(filter.FullName))
//         {
//             query = query.Where(x => x.FullName.Contains(filter.FullName));
//         }
//
//         if (!string.IsNullOrEmpty(filter.Phone))
//         {
//             query = query.Where(x => x.PhoneNumber != null && x.PhoneNumber.Contains(filter.Phone));
//         }
//
//         if (filter.Role.HasValue)
//         {
//             string roleName = filter.Role.Value.ToString(); 
//
//             query = query.Where(u => context.UserRoles
//                 .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name })
//                 .Any(ur => ur.UserId == u.Id && ur.RoleName == roleName));
//         }
//
//
//         var totalRecords = await query.CountAsync();
//         var skip = (filter.PageNumber - 1) * filter.PageSize;
//         var usersList = await query.OrderBy(x => x.Id)
//             .Skip(skip)
//             .Take(filter.PageSize)
//             .ToListAsync();
//
//         var userDtos = new List<GetUserDto>();
//         foreach (var user in usersList)
//         {
//             var roles = await userManager.GetRolesAsync(user);
//             var role = roles.FirstOrDefault(); 
//             userDtos.Add(new GetUserDto
//             {
//                 FullName = user.FullName,
//                 PhoneNumber = user.PhoneNumber,
//                 Email = user.Email,
//                 Address = user.Address,
//                 Gender = user.Gender,
//                 ActiveStatus = user.ActiveStatus,
//                 Role = role
//             });
//         }
//
//         return new PaginationResponse<List<GetUserDto>>(userDtos, totalRecords, filter.PageNumber, filter.PageSize);
//     }
//     #endregion
//
//     
//     #region GetUserById
//     
//     public async Task<Response<GetUserDto>> GetUserByIdAsync(int id)
//     {
//         var user = await context.Users.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
//         if (user == null)
//         {
//             return new Response<GetUserDto>(System.Net.HttpStatusCode.NotFound, "User not found");
//         }
//
//         var roles = await userManager.GetRolesAsync(user);
//         var role = roles.FirstOrDefault();
//
//         var dto = new GetUserDto
//         {
//             FullName = user.FullName,
//             PhoneNumber = user.PhoneNumber,
//             Email = user.Email,
//             Address = user.Address,
//             Gender = user.Gender,
//             ActiveStatus = user.ActiveStatus,
//             Role = role
//         };
//
//         return new Response<GetUserDto>(dto);
//     }
//     
//     #endregion
//
//     #region GetCurrentUser
//     
//     public async Task<Response<GetUserDto>> GetCurrentUserAsync()
//     {
//         var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
//         if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
//         {
//             return new Response<GetUserDto>(System.Net.HttpStatusCode.Unauthorized, "User not authenticated");
//         }
//         
//         return await GetUserByIdAsync(id);
//     }
//     
//     #endregion
// }
