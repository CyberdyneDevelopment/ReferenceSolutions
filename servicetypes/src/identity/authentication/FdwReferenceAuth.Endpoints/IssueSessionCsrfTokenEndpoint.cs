using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Sessions;
using FdwReferenceAuth.Endpoints.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Issues the CSRF token a session-signed-in browser app echoes on each write, for a Session service
/// whose protection is SynchronizerToken (FDW-903).
/// </summary>
/// <remarks>
/// A read, so it is not itself held to CSRF protection, and safe to be one: another site can make the
/// browser send the cookie here, but SameSite and CORS keep it from reading the answer. Only the
/// token's hash is kept, on the session row, so each call issues a new token and retires the last.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class IssueSessionCsrfTokenEndpoint : EndpointWithoutRequest<SessionCsrfTokenResponse>
{
    private readonly ISessionCookieDelivery _sessions;
    private readonly ILogger<IssueSessionCsrfTokenEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="IssueSessionCsrfTokenEndpoint"/> class.</summary>
    /// <param name="sessions">Issues the token for the session behind the request.</param>
    /// <param name="logger">The logger.</param>
    public IssueSessionCsrfTokenEndpoint(ISessionCookieDelivery sessions, ILogger<IssueSessionCsrfTokenEndpoint>? logger = null)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _logger = logger ?? NullLogger<IssueSessionCsrfTokenEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/auth/session/csrf");
        Summary(s =>
        {
            s.Summary = "Issue a CSRF token for the current session";
            s.Description = "For a session signed in through a Session service whose CSRF protection is SynchronizerToken: returns a new token to echo on every write. Refused for a request not authenticated by a session.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var token = await _sessions.IssueCsrfToken(HttpContext, ct).ConfigureAwait(false);
        if (token.IsFailure || token.Value is not { } issued)
        {
            AuthenticationLog.SessionCsrfTokenRefused(_logger);
            await Send.ResponseAsync(new SessionCsrfTokenResponse(), StatusCodes.Status400BadRequest, ct).ConfigureAwait(false);
            return;
        }

        AuthenticationLog.SessionCsrfTokenIssued(_logger);
        await Send.OkAsync(new SessionCsrfTokenResponse { Token = issued }, ct).ConfigureAwait(false);
    }
}
