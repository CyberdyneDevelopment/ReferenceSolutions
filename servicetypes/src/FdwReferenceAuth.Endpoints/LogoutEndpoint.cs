using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Endpoints;
using Fdw.Services.Authentication.Sessions;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to log out the current user (POST /auth/logout).
/// </summary>
/// <remarks>
/// The abstract <see cref="LogoutEndpointBase"/> ships in FDW but registers no route unless a concrete
/// subclass exists in the host app — without this class the route returned 404.
/// <para>
/// Revokes the presented token directly through <see cref="ITokenRevocationStore"/> — no step-pipeline
/// involvement. <c>AuthenticationRunner.Terminal</c> unconditionally mints a new token on every
/// successful flow, which is backwards for logout, so this bypasses the pipeline the same way
/// <c>JwtIssuanceResolver</c> already bypasses the (dead) <c>TokenManagerTypes</c> dispatch for
/// issuance — nothing in the existing mechanism fits, and building a non-issuing terminal mode for the
/// runner is separate, larger work.
/// </para>
/// <para>
/// This is an authenticated endpoint, so <c>User</c> is already the validated
/// <see cref="System.Security.Claims.ClaimsPrincipal"/> the presented bearer token decoded to — jti and
/// exp are read off it directly rather than re-parsing the raw token.
/// </para>
/// </remarks>
public class LogoutEndpoint : LogoutEndpointBase
{
    private readonly ITokenRevocationStore _revocations;
    private readonly ISessionCookieDelivery _sessions;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutEndpoint"/> class.
    /// </summary>
    public LogoutEndpoint(ITokenRevocationStore revocations, ISessionCookieDelivery sessions, ILogger<LogoutEndpoint> logger)
        : base(logger)
    {
        _revocations = revocations ?? throw new ArgumentNullException(nameof(revocations));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult> PerformLogout(string? username, CancellationToken ct)
    {
        // A server-side session (FDW-903) is ended where it lives: its row is revoked and its cookie
        // cleared. It carries no jti, so the token path below does not apply to it.
        if (_sessions.IsSessionRequest(HttpContext))
            return await _sessions.End(HttpContext, ct).ConfigureAwait(false);

        // JwtTokenIssuer mints jti unconditionally and always sets Expires, so a token missing either
        // did not come from this host's own issuer: it cannot be revoked, and saying it was would be
        // a lie. Both that and a failed revocation are returned, and the base answers with them
        // instead of 204 (FDW-875).
        if (User.FindFirst(ClaimDefinitions.jti.Name)?.Value is not { Length: > 0 } jtiClaim
            || !Guid.TryParse(jtiClaim, out var jti)
            || User.FindFirst("exp")?.Value is not { Length: > 0 } expClaim
            || !long.TryParse(expClaim, out var expUnix))
        {
            return GenericResult.Failure(AuthenticationEndpointLog.AuthenticationFailed(EndpointLogger, username));
        }

        return await _revocations.Revoke(jti, DateTimeOffset.FromUnixTimeSeconds(expUnix), ct)
            .ConfigureAwait(false);
    }
}
