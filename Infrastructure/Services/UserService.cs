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

public class UserService(DataContext context, UserManager<User> userManager, IHttpContextAccessor httpContextAccessor) : IUserService
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
                    Image = user.ProfileImagePath,
                    Role = role
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
                Image = user.ProfileImagePath,
                Role = role
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
                    Role = role
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
                Image = user.ProfileImagePath,
                Role = role
            }).ToList();
            
            return new Response<List<GetUserDto>>(userDtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GetUserDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    
    #endregion
    
    // #region GetUserActivity
    //
    // public async Task<Response<UserActivityDto>> GetUserActivityAsync(int userId)
    // {
    //     try
    //     {
    //         var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
    //         if (user == null)
    //         {
    //             return new Response<UserActivityDto>(HttpStatusCode.NotFound, "User not found");
    //         }
    //         
    //         // Создаем базовый объект активности пользователя
    //         var activityDto = new UserActivityDto
    //         {
    //             UserId = userId,
    //             Username = user.UserName ?? user.Email ?? user.FullName,
    //             LastLoginTime = DateTime.UtcNow, // Заполним позже, если найдем реальные данные
    //             LoginCount = 0,
    //             TotalActions = 0,
    //             LastActivityTime = user.UpdatedAt.DateTime
    //         };
    //
    //         // 1. Собираем уведомления, связанные с пользователем (студентом или ментором)
    //         var notificationLogs = new List<NotificationLog>();
    //         
    //         // Если пользователь - студент
    //         if (user.StudentProfile != null)
    //         {
    //             notificationLogs.AddRange(await context.NotificationLogs
    //                 .Where(n => n.StudentId == user.StudentProfile.Id && !n.IsDeleted)
    //                 .OrderByDescending(n => n.SentDateTime)
    //                 .Take(10)
    //                 .ToListAsync());
    //                 
    //             // Добавляем записи о действиях, связанных со студентом
    //             foreach (var notification in notificationLogs)
    //             {
    //                 activityDto.RecentActions.Add(new UserActivityDto.UserActionItem
    //                 {
    //                     Timestamp = notification.SentDateTime,
    //                     ActionType = "Notification",
    //                     Description = notification.Subject,
    //                     RelatedEntityType = "Student",
    //                     RelatedEntityId = notification.StudentId
    //                 });
    //             }
    //             
    //             // Активность, связанная с группами студента
    //             var studentGroups = await context.StudentGroups
    //                 .Where(sg => sg.StudentId == user.StudentProfile.Id && !sg.IsDeleted)
    //                 .OrderByDescending(sg => sg.UpdatedAt)
    //                 .Take(5)
    //                 .ToListAsync();
    //                 
    //             foreach (var sg in studentGroups)
    //             {
    //                 activityDto.RecentActions.Add(new UserActivityDto.UserActionItem
    //                 {
    //                     Timestamp = sg.UpdatedAt.DateTime,
    //                     ActionType = sg.IsActive == true ? "JoinGroup" : "LeaveGroup",
    //                     Description = sg.IsActive == true ? "Joined a group" : "Left a group",
    //                     RelatedEntityType = "Group",
    //                     RelatedEntityId = sg.GroupId
    //                 });
    //             }
    //         }
    //         
    //         // Если пользователь - ментор
    //         if (user.MentorProfile != null)
    //         {
    //             // Активность, связанная с группами ментора
    //             var mentorGroups = await context.MentorGroups
    //                 .Where(mg => mg.MentorId == user.MentorProfile.Id && !mg.IsDeleted)
    //                 .OrderByDescending(mg => mg.UpdatedAt)
    //                 .Take(5)
    //                 .ToListAsync();
    //                 
    //             foreach (var mg in mentorGroups)
    //             {
    //                 activityDto.RecentActions.Add(new UserActivityDto.UserActionItem
    //                 {
    //                     Timestamp = mg.UpdatedAt.DateTime,
    //                     ActionType = "AssignedToGroup",
    //                     Description = "Assigned to a group as mentor",
    //                     RelatedEntityType = "Group",
    //                     RelatedEntityId = mg.GroupId
    //                 });
    //             }
    //         }
    //         
    //         // 2. Собираем данные из таблицы комментариев
    //         var comments = await context.Comments
    //             .Where(c => c.AuthorId == userId && !c.IsDeleted)
    //             .OrderByDescending(c => c.CreatedAt)
    //             .Take(10)
    //             .ToListAsync();
    //             
    //         foreach (var comment in comments)
    //         {
    //             activityDto.RecentActions.Add(new UserActivityDto.UserActionItem
    //             {
    //                 Timestamp = comment.CreatedAt.DateTime,
    //                 ActionType = "AddComment",
    //                 Description = $"Added comment: {(comment.Text.Length > 30 ? comment.Text.Substring(0, 30) + "..." : comment.Text)}",
    //                 RelatedEntityType = comment.EntityType.ToString(),
    //                 RelatedEntityId = comment.EntityId
    //             });
    //         }
    //         
    //         // 3. Имитация логинов (в будущем можно заменить на реальные данные из таблицы логинов)
    //         activityDto.RecentLogins.Add(new UserActivityDto.LoginHistoryItem 
    //         { 
    //             LoginTime = DateTime.UtcNow.AddDays(-1), 
    //             IpAddress = "192.168.1.1", 
    //             UserAgent = "Chrome/121.0.0.0", 
    //             IsSuccessful = true 
    //         });
    //         
    //         activityDto.RecentLogins.Add(new UserActivityDto.LoginHistoryItem 
    //         { 
    //             LoginTime = DateTime.UtcNow.AddDays(-3), 
    //             IpAddress = "192.168.1.1", 
    //             UserAgent = "Firefox/120.0", 
    //             IsSuccessful = true 
    //         });
    //         
    //         // 4. Подсчитываем статистику действий по категориям
    //         var categoryCount = activityDto.RecentActions
    //             .GroupBy(a => a.ActionType)
    //             .ToDictionary(g => g.Key, g => g.Count());
    //             
    //         foreach (var category in categoryCount)
    //         {
    //             activityDto.ActivityByCategory[category.Key] = category.Value;
    //         }
    //         
    //         // Если каких-то категорий нет, добавляем их для полноты данных
    //         if (!activityDto.ActivityByCategory.ContainsKey("Login"))
    //             activityDto.ActivityByCategory["Login"] = 2; // Количество логинов из имитации
    //             
    //         if (!activityDto.ActivityByCategory.ContainsKey("Profile"))
    //             activityDto.ActivityByCategory["Profile"] = 1;
    //         
    //         // 5. Обновляем общую статистику
    //         activityDto.TotalActions = activityDto.RecentActions.Count + activityDto.RecentLogins.Count;
    //         activityDto.LoginCount = activityDto.RecentLogins.Count;
    //         
    //         if (activityDto.RecentLogins.Any())
    //             activityDto.LastLoginTime = activityDto.RecentLogins.Max(l => l.LoginTime);
    //             
    //         if (activityDto.RecentActions.Any())
    //         {
    //             var lastAction = activityDto.RecentActions.MaxBy(a => a.Timestamp);
    //             if (lastAction != null && lastAction.Timestamp > activityDto.LastLoginTime)
    //                 activityDto.LastActivityTime = lastAction.Timestamp;
    //             else
    //                 activityDto.LastActivityTime = activityDto.LastLoginTime;
    //         }
    //         else
    //         {
    //             activityDto.LastActivityTime = activityDto.LastLoginTime;
    //         }
    //
    //         return new Response<UserActivityDto>(activityDto);
    //     }
    //     catch (Exception ex)
    //     {
    //         return new Response<UserActivityDto>(HttpStatusCode.InternalServerError, ex.Message);
    //     }
    // }
    //
    // #endregion
}
