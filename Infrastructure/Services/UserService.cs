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
using Infrastructure.Helpers;
using Infrastructure.Constants;
using Microsoft.AspNetCore.Hosting;

namespace Infrastructure.Services;

public class UserService(DataContext context, UserManager<User> userManager,
    IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment, RoleManager<IdentityRole<int>> roleManager) : IUserService
{
    #region GetUsersAsync

    public async Task<PaginationResponse<List<GetUserDto>>> GetUsersAsync(UserFilter filter)
    {
        try
        {
            var query = context.Users.Where(x => !x.IsDeleted).AsQueryable();
            query = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(query, httpContextAccessor, u => u.CenterId);

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
                var dto = await DtoMappingHelper.MapToGetUserDtoAsync(user, userManager);
                userDtos.Add(dto);
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

    #region GetUserByIdAsync

    public async Task<Response<GetUserDto>> GetUserByIdAsync(int id)
    {
        try
        {
            var query = context.Users.Where(x => x.Id == id && !x.IsDeleted);
            query = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(query, httpContextAccessor, u => u.CenterId);
            var user = await query.FirstOrDefaultAsync();
            
            if (user == null)
            {
                return new Response<GetUserDto>(HttpStatusCode.NotFound, Messages.User.NotFound);
            }

            var dto = await DtoMappingHelper.MapToGetUserDtoAsync(user, userManager);
            return new Response<GetUserDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetUserDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetCurrentUserAsync

    public async Task<Response<GetUserDetailsDto>> GetCurrentUserAsync()
    {
        try
        {
            var principalIdRaw = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                ?? httpContextAccessor.HttpContext?.User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(principalIdRaw) || !int.TryParse(principalIdRaw, out int principalId))
            {
                return new Response<GetUserDetailsDto>(HttpStatusCode.Unauthorized, Messages.User.UserNotAuthenticated);
            }

            var userIdRaw = httpContextAccessor.HttpContext?.User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdRaw) || !int.TryParse(userIdRaw, out int userId))
            {
                return new Response<GetUserDetailsDto>(HttpStatusCode.Unauthorized, Messages.User.UserIdNotFoundInToken);
            }

            var query = context.Users
                .Include(u => u.Center)
                .Where(x => x.Id == userId && !x.IsDeleted);

            var user = await query.FirstOrDefaultAsync();

            if (user == null)
                return new Response<GetUserDetailsDto>(HttpStatusCode.NotFound, Messages.User.NotFound);

            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            var dto = DtoMappingHelper.MapToGetUserDetailsDto(user, principalId, role);
            return new Response<GetUserDetailsDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetUserDetailsDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region SearchUsersAsync

    public async Task<Response<List<GetUserDto>>> SearchUsersAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new Response<List<GetUserDto>>(HttpStatusCode.BadRequest, Messages.User.SearchTermRequired);
            }

            var query = context.Users
                .Where(u => !u.IsDeleted &&
                           (u.FullName.Contains(searchTerm) ||
                           (u.Email != null && u.Email.Contains(searchTerm)) ||
                           (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm))));
            query = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(query, httpContextAccessor, u => u.CenterId);
            var users = await query.Take(20).ToListAsync();

            if (!users.Any())
            {
                return new Response<List<GetUserDto>>(HttpStatusCode.NotFound, Messages.User.NoUsersFound);
            }

            var userDtos = new List<GetUserDto>();
            foreach (var user in users)
            {
                var dto = await DtoMappingHelper.MapToGetUserDtoAsync(user, userManager);
                userDtos.Add(dto);
            }

            return new Response<List<GetUserDto>>(userDtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GetUserDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetUsersByRoleAsync

    public async Task<Response<List<GetUserDto>>> GetUsersByRoleAsync(string role)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return new Response<List<GetUserDto>>(HttpStatusCode.BadRequest, Messages.User.RoleRequired);
            }

            var roleObj = await context.Roles.FirstOrDefaultAsync(r => r.Name == role);
            if (roleObj == null)
            {
                return new Response<List<GetUserDto>>(HttpStatusCode.NotFound, string.Format(Messages.User.RoleNotFound, role));
            }

            var userIds = await context.UserRoles
                .Where(ur => ur.RoleId == roleObj.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var query = context.Users
                .Where(u => !u.IsDeleted && userIds.Contains(u.Id));
            query = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(query, httpContextAccessor, u => u.CenterId);
            var users = await query.ToListAsync();

            if (!users.Any())
            {
                return new Response<List<GetUserDto>>(HttpStatusCode.NotFound, string.Format(Messages.User.NoUsersWithRole, role));
            }

            var userDtos = users.Select(user => DtoMappingHelper.MapToGetUserDtoSync(user, role)).ToList();
            return new Response<List<GetUserDto>>(userDtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GetUserDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetUpcomingBirthdaysAsync

    public async Task<PaginationResponse<List<GetUserDto>>> GetUpcomingBirthdaysAsync(int page, int pageSize)
    {
        var today = DateTime.Today;
        var end = today.AddDays(7);
        var query = context.Users
            .Where(u => !u.IsDeleted &&
                (
                    (u.Birthday.Month == today.Month && u.Birthday.Day >= today.Day) ||
                    (u.Birthday.Month == end.Month && u.Birthday.Day <= end.Day) ||
                    (u.Birthday.Month > today.Month && u.Birthday.Month < end.Month)
                )
            );

        var studentRoleId = await roleManager.FindByNameAsync("Student");
        if (studentRoleId != null)
        {
            query = query.Where(u => !context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId.ToString() == studentRoleId.Id.ToString()));
        }

        query = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(query, httpContextAccessor, u => u.CenterId);
        var users = await query.OrderBy(u => u.Birthday.Month)
            .ThenBy(u => u.Birthday.Day)
            .ToListAsync();

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
            var dto = await DtoMappingHelper.MapToGetUserDtoAsync(user, userManager);
            result.Add(dto);
        }
        
        return new PaginationResponse<List<GetUserDto>>(result, page, pageSize, total);
    }

    #endregion

    #region UpdateProfilePictureAsync

    public async Task<Response<string>> UpdateProfilePictureAsync(UpdateProfilePictureDto updateProfilePictureDto)
    {
        try
        {
            var currentUserIdRaw = httpContextAccessor.HttpContext?.User.FindFirst("UserId")?.Value
                                 ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdRaw) || !int.TryParse(currentUserIdRaw, out int currentUserId))
                return new Response<string>(HttpStatusCode.Unauthorized, Messages.User.UserNotAuthenticated);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId && !u.IsDeleted);
            if (user == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.User.NotFound);

            var imageResult = await FileUploadHelper.UploadFileAsync(
                updateProfilePictureDto.ProfilePicture,
                webHostEnvironment.WebRootPath,
                "profiles",
                "profile",
                true,
                user.ProfileImagePath
            );

            if (imageResult.StatusCode != (int)HttpStatusCode.OK)
                return new Response<string>((HttpStatusCode)imageResult.StatusCode, imageResult.Message);

            user.ProfileImagePath = imageResult.Data;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return new Response<string>(HttpStatusCode.BadRequest, IdentityHelper.FormatIdentityErrors(result));

            return new Response<string>(HttpStatusCode.OK, Messages.User.ProfileImageUpdated);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.User.ProfileImageUpdateError, ex.Message));
        }
    }

    #endregion

    #region ChangeEmailAsync

    public async Task<Response<string>> ChangeEmailAsync(int userId, string newEmail)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, Messages.User.NotFound);
            }

            var existingUserWithEmail = await userManager.FindByEmailAsync(newEmail);
            if (existingUserWithEmail != null && existingUserWithEmail.Id != user.Id)
            {
                return new Response<string>(HttpStatusCode.BadRequest, Messages.User.EmailAlreadyInUse);
            }

            var setEmailResult = await userManager.SetEmailAsync(user, newEmail);
            if (!setEmailResult.Succeeded)
            {
                return new Response<string>(HttpStatusCode.InternalServerError, IdentityHelper.FormatIdentityErrors(setEmailResult));
            }

            user.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.OK, Messages.User.EmailUpdated);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, string.Format(Messages.User.EmailUpdateError, ex.Message));
        }
    }

    #endregion
}
