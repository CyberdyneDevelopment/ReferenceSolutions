using System.Threading.Tasks;
using Fdw.Web.Http.Authentication;
using Microsoft.AspNetCore.Http;
using ReferenceAuthentication.TokenProviders;
using Shouldly;
using Xunit;

namespace ReferenceAuthentication.TokenProviders.Tests;

/// <summary>
/// Covers <see cref="ForwardedUserTokenProvider"/> — the delegation path where a proxy forwards the
/// caller's own bearer token downstream rather than acquiring a service identity of its own.
/// </summary>
public class ForwardedUserTokenProviderTests
{
    private static ForwardedUserTokenProvider WithHeader(string? authorization)
    {
        var context = new DefaultHttpContext();
        if (authorization is not null)
            context.Request.Headers.Authorization = authorization;

        return new ForwardedUserTokenProvider(new HttpContextAccessor { HttpContext = context });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public async Task ForwardsTheBearerTokenFromTheInboundRequest()
    {
        var token = await WithHeader("Bearer abc.def.ghi").GetAccessToken(TestContext.Current.CancellationToken);

        token.ShouldBe("abc.def.ghi");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public async Task MatchesTheBearerSchemeCaseInsensitively()
    {
        // Why: the scheme is case-insensitive per RFC 7235; a client sending "bearer" is conformant.
        var token = await WithHeader("bearer abc.def.ghi").GetAccessToken(TestContext.Current.CancellationToken);

        token.ShouldBe("abc.def.ghi");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Authentication")]
    public async Task TrimsSurroundingWhitespaceFromTheToken()
    {
        var token = await WithHeader("Bearer   abc.def.ghi  ").GetAccessToken(TestContext.Current.CancellationToken);

        token.ShouldBe("abc.def.ghi");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Basic dXNlcjpwYXNz")]
    [InlineData("abc.def.ghi")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public async Task ReturnsNullWhenThereIsNoBearerTokenToForward(string? authorization)
    {
        // Why null rather than empty: BearerTokenHandler sends no Authorization header at all for
        // null, so the downstream returns 401 rather than rejecting a malformed empty credential.
        var token = await WithHeader(authorization).GetAccessToken(TestContext.Current.CancellationToken);

        token.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public async Task ReturnsNullWhenThereIsNoHttpContext()
    {
        // Why: a background job has no inbound request. There is no token to delegate, and inventing
        // a service identity here would silently widen the proxy's authority beyond the caller's.
        var provider = new ForwardedUserTokenProvider(new HttpContextAccessor { HttpContext = null });

        var token = await provider.GetAccessToken(TestContext.Current.CancellationToken);

        token.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Authentication")]
    public void IsAnAccessTokenProvider()
    {
        // Why assert the contract: this type exists to be resolved as IAccessTokenProvider by
        // BearerTokenHandler. Losing the interface would compile and fail at composition.
        WithHeader("Bearer x").ShouldBeAssignableTo<IAccessTokenProvider>();
    }
}
