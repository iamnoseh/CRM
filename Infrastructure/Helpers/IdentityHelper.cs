using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Helpers;

public static class IdentityHelper
{
    public static string FormatIdentityErrors(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(e => e.Description));
    }
}