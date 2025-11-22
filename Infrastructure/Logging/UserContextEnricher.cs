using Serilog.Core;
using Serilog.Events;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Logging;

public class UserContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = httpContext.User.FindFirst(ClaimTypes.Name)?.Value 
                          ?? httpContext.User.FindFirst("FullName")?.Value;
            var userRole = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            var centerId = httpContext.User.FindFirst("CenterId")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
            }

            if (!string.IsNullOrEmpty(userName))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserName", userName));
            }

            if (!string.IsNullOrEmpty(userRole))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserRole", userRole));
            }

            if (!string.IsNullOrEmpty(centerId))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CenterId", centerId));
            }

            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("IpAddress", ipAddress));
            }
        }
    }
}
