using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.Http.Authentication;
using Microsoft.AspNetCore.Http;

namespace ReferenceAuthentication.TokenProviders;

/// <summary>
/// <see cref="IAccessTokenProvider"/> that forwards the incoming request's bearer token to
/// downstream services (ETL / Scheduler) for the proxy endpoints. This is delegation: the
/// downstream validates the SAME user token and enforces the user's own permissions, so the
/// proxy never needs a broader service identity than the caller already holds.
/// Why: the previous client-credentials token (fdw.api) carried no perm claims, so once the
/// downstream endpoints enforce permissions it was rejected (403). Forwarding the user's token
/// makes the proxy authorize exactly as a direct call would.
/// </summary>
public sealed class ForwardedUserTokenProvider : IAccessTokenProvider
{
    private const string BearerPrefix = "Bearer ";
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForwardedUserTokenProvider"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the inbound request whose bearer token is forwarded.</param>
    public ForwardedUserTokenProvider(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public Task<string?> GetAccessToken(CancellationToken cancellationToken = default)
    {
        var header = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(header) &&
            header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<string?>(header[BearerPrefix.Length..].Trim());
        }

        // Why: no inbound bearer token means the caller is unauthenticated — fail loud with null
        // so BearerTokenHandler sends no Authorization header and the downstream returns 401.
        return Task.FromResult<string?>(null);
    }
}
