using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;
using System.Linq.Expressions;

namespace Infrastructure.Helpers;

public static class QueryFilterHelper
{
    public static IQueryable<T> FilterByCenterIfNotSuperAdmin<T>(
        IQueryable<T> query,
        IHttpContextAccessor httpContextAccessor,
        Expression<Func<T, int?>> centerIdSelector)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var roles = user?.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);

        bool isSuperAdmin = roles != null && roles.Contains("SuperAdmin");

        if (!isSuperAdmin && centerId != null)
        {
            var parameter = centerIdSelector.Parameters[0];
            var body = Expression.Equal(centerIdSelector.Body, Expression.Constant(centerId, typeof(int?)));
            var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
            query = query.Where(lambda);
        }
        return query;
    }
}