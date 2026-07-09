using System.Security.Claims;
using LogisticsHub.AspNetCore;
using Xunit;

namespace LogisticsHub.AspNetCore.Tests;

public sealed class ApiScopeAuthorizationTests
{
    private const string ExpectedScope = "api://test-api/access_as_user";

    [Fact]
    public void HasRequiredScope_ReturnsTrue_WhenScpClaimContainsExpectedScope()
    {
        var user = CreateUser(new Claim("scp", ExpectedScope));

        var result = ApiScopeAuthorization.HasRequiredScope(user, ExpectedScope);

        Assert.True(result);
    }

    [Fact]
    public void HasRequiredScope_ReturnsTrue_WhenScopeClaimContainsExpectedScope()
    {
        var user = CreateUser(new Claim("scope", ExpectedScope));

        var result = ApiScopeAuthorization.HasRequiredScope(user, ExpectedScope);

        Assert.True(result);
    }

    [Fact]
    public void HasRequiredScope_ReturnsFalse_WhenScopeClaimIsMissing()
    {
        var user = CreateUser(new Claim(ClaimTypes.NameIdentifier, "user-1"));

        var result = ApiScopeAuthorization.HasRequiredScope(user, ExpectedScope);

        Assert.False(result);
    }

    [Fact]
    public void HasRequiredScope_ReturnsFalse_WhenScopeClaimDoesNotMatchExpectedScope()
    {
        var user = CreateUser(new Claim("scp", "api://test-api/read_only"));

        var result = ApiScopeAuthorization.HasRequiredScope(user, ExpectedScope);

        Assert.False(result);
    }

    [Fact]
    public void HasRequiredScope_ReturnsTrue_WhenSpaceSeparatedScopesContainExpectedScope()
    {
        var user = CreateUser(new Claim("scp", $"openid profile {ExpectedScope} offline_access"));

        var result = ApiScopeAuthorization.HasRequiredScope(user, ExpectedScope);

        Assert.True(result);
    }

    private static ClaimsPrincipal CreateUser(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }
}
