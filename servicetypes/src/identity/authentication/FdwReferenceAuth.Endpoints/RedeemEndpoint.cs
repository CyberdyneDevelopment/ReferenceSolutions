using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Execution;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceAuth.Endpoints.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Spends the one-time code <see cref="OidcCallbackEndpoint"/> returned the browser with, answering
/// the same token envelope as <see cref="PasswordLoginEndpoint"/>.
/// </summary>
/// <remarks>
/// A code redeems exactly once and lives about a minute. A missing, expired, and already-redeemed
/// code all answer 400 identically, so a caller cannot learn that a code was ever real.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class RedeemEndpoint : Endpoint<RedeemRequest, PasswordLoginResponse>
{
    private readonly IAuthorizationCodeStore _codes;
    private readonly IAuthenticationFlowProvider _flows;
    private readonly ISessionCookieDelivery _sessions;
    private readonly ILogger<RedeemEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="RedeemEndpoint"/> class.</summary>
    /// <param name="codes">Holds issued tokens behind one-time codes.</param>
    /// <param name="flows">Reads the flow that issued a code, which decides what redeeming it returns.</param>
    /// <param name="sessions">Sets the session cookie when that flow delivers a session (FDW-903).</param>
    /// <param name="logger">The logger.</param>
    public RedeemEndpoint(
        IAuthorizationCodeStore codes,
        IAuthenticationFlowProvider flows,
        ISessionCookieDelivery sessions,
        ILogger<RedeemEndpoint>? logger = null)
    {
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _logger = logger ?? NullLogger<RedeemEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/auth/redeem");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(RedeemRequest req, CancellationToken ct)
    {
        var redeemed = await _codes.Redeem(req.AuthCode, ct).ConfigureAwait(false);
        if (redeemed.IsFailure || redeemed.Value is not { } code)
        {
            AuthenticationLog.RedeemRefused(_logger);
            await Send.ResponseAsync(new PasswordLoginResponse(), StatusCodes.Status400BadRequest, ct)
                .ConfigureAwait(false);
            return;
        }

        // A session-delivered flow's code carries a session id (FDW-903). This request comes from the
        // browser app's own origin, so the cookie set here is on the origin every later call goes to;
        // the id itself never reaches the body.
        var flow = await _flows.Get(code.FlowName, ct).ConfigureAwait(false);
        if (flow.IsFailure || flow.Value is null)
        {
            AuthenticationLog.RedeemSessionNotDelivered(_logger, code.FlowName);
            await Send.ResponseAsync(new PasswordLoginResponse(), StatusCodes.Status500InternalServerError, ct)
                .ConfigureAwait(false);
            return;
        }

        if (flow.Value.SessionServiceName is not null)
        {
            var delivered = await _sessions.Deliver(HttpContext, flow.Value, code.Token, ct).ConfigureAwait(false);
            if (delivered.IsFailure)
            {
                AuthenticationLog.RedeemSessionNotDelivered(_logger, code.FlowName);
                await Send.ResponseAsync(new PasswordLoginResponse(), StatusCodes.Status500InternalServerError, ct)
                    .ConfigureAwait(false);
                return;
            }

            AuthenticationLog.Redeemed(_logger);
            await Send.OkAsync(
                new PasswordLoginResponse { TokenType = code.Token.TokenType, ExpiresAt = code.Token.ExpiresAt },
                ct).ConfigureAwait(false);
            return;
        }

        AuthenticationLog.Redeemed(_logger);
        await Send.OkAsync(
            new PasswordLoginResponse
            {
                AccessToken = code.Token.AccessToken,
                TokenType = code.Token.TokenType,
                ExpiresAt = code.Token.ExpiresAt,
            },
            ct).ConfigureAwait(false);
    }
}
