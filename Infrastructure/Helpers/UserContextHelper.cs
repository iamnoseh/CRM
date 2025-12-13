using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Helpers;

public static class UserContextHelper
{
    public static int? GetCurrentUserCenterId(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var roles = user?.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        if (roles != null && roles.Contains("SuperAdmin"))
            return null;
        var centerIdClaim = user?.Claims.FirstOrDefault(c => c.Type == "CenterId")?.Value;
        if (int.TryParse(centerIdClaim, out int centerId))
            return centerId;
        return null;
    }

    public static int? GetCurrentUserMentorId(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var mentorIdClaim = user?.Claims.FirstOrDefault(c => c.Type == "MentorId")?.Value;
        if (int.TryParse(mentorIdClaim, out int mentorId))
            return mentorId;
        return null;
    }

    public static int? GetCurrentUserId(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var userIdClaim = user?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
            return userId;
        return null;
    }
}