using Microsoft.AspNetCore.Http;

namespace Infrastructure.Helpers;

public static class UserContextHelper
{
    public static int? GetCurrentUserCenterId(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var centerIdClaim = user?.Claims.FirstOrDefault(c => c.Type == "CenterId")?.Value;
        if (int.TryParse(centerIdClaim, out int centerId))
            return centerId;
        return null;
    }
} 