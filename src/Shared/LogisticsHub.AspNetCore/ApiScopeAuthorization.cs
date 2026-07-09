using System.Security.Claims;

namespace LogisticsHub.AspNetCore;

public static class ApiScopeAuthorization
{
    public static bool HasRequiredScope(ClaimsPrincipal user, string requiredScope)
    {
        if (string.IsNullOrWhiteSpace(requiredScope))
        {
            return false;
        }

        return user.Claims
            .Where(claim => claim.Type is "scp" or "scope")
            .SelectMany(claim => claim.Value.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(scope => string.Equals(scope, requiredScope, StringComparison.Ordinal));
    }
}
