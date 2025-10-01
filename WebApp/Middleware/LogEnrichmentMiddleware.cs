using System.Security.Claims;
using Serilog.Context;

namespace WebApp.Middleware;

public class LogEnrichmentMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        var userName = user?.Identity?.IsAuthenticated == true
            ? (user.FindFirst(ClaimTypes.Name)?.Value ?? context.User.Identity?.Name ?? "unknown")
            : "anonymous";
        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "-";
        var centerId = user?.FindFirst("CenterId")?.Value ?? "-";
        var roles = string.Join(',', user?.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value) ?? Array.Empty<string>());

        using (LogContext.PushProperty("UserName", userName))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("CenterId", centerId))
        using (LogContext.PushProperty("UserRoles", roles))
        {
            await _next(context);
        }
    }
}


