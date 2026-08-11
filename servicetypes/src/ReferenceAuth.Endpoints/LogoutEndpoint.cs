using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Endpoints;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to log out the current user (POST /auth/logout).
/// </summary>
/// <remarks>
/// The abstract <see cref="LogoutEndpointBase"/> ships in FDW but registers no route unless a concrete
/// subclass exists in the host app — without this class the route returned 404.
/// <para>
/// Delegates to <see cref="IAuthenticationService.Logout(string, CancellationToken)"/>, which revokes
/// every server-side session/authorization for the token's subject and deny-lists the presented token
/// itself.
/// </para>
/// </remarks>
public class LogoutEndpoint : LogoutEndpointBase
{
    private const string BearerPrefix = "Bearer ";

    private readonly IAuthenticationService _authenticationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutEndpoint"/> class.
    /// </summary>
    public LogoutEndpoint(
        IAuthenticationService authenticationService,
        IHttpContextAccessor httpContextAccessor,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _authenticationService = authenticationService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    protected override async Task PerformLogout(string username, CancellationToken ct)
    {
        var header = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            // Why: no inbound bearer token means there is nothing to log out — log and return,
            // the base still responds 204 (best-effort logout; the client discards its token regardless).
            AuthenticationEndpointLog.AuthenticationFailed(EndpointLogger, username);
            return;
        }

        var token = header[BearerPrefix.Length..].Trim();

        var result = await _authenticationService.Logout(token, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            // Why: surface logout failure in the trace; the base still returns 204 on a best-effort
            // logout (the client's token is discarded regardless), but the failure is logged.
            AuthenticationEndpointLog.AuthenticationFailed(EndpointLogger, username);
        }
    }
}
