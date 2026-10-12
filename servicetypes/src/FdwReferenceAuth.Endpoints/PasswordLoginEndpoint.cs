using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authentication.Sessions;
using Fdw.Services.Users.Results;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Logs a caller in with a username and password.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint runs a flow and returns what it produced. Which steps that flow has — a password
/// check, a permission bake, an eligibility decision, and a second factor if one is ever added — is
/// configuration, so adding one changes a row rather than this file.
/// </para>
/// <para>
/// A failed login returns 401 with no detail. Which of "no such user", "wrong password" or "account
/// disabled" occurred is in the log for an operator, and out of the response so it cannot be used to
/// enumerate accounts.
/// </para>
/// <para>
/// A login the flow accepted and then could not issue a token or session for returns 500, not 401: the
/// credentials were right, and the fault is this host's (FDW-918).
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class PasswordLoginEndpoint : Endpoint<PasswordLoginRequest, PasswordLoginResponse>
{
    private readonly AuthenticationRunner _runner;
    private readonly IAuthenticationFlowProvider _flows;
    private readonly ISessionCookieDelivery _sessions;
    private readonly PresentedPasswordCredentialAccessor _presented;

    /// <summary>Initializes a new instance of the <see cref="PasswordLoginEndpoint"/> class.</summary>
    /// <param name="runner">Runs the flow.</param>
    /// <param name="flows">Supplies the flow to run.</param>
    /// <param name="sessions">Sets the session cookie when the flow delivers a session (FDW-903).</param>
    /// <param name="presented">Presents the request's credential to the flow's credential step.</param>
    public PasswordLoginEndpoint(
        AuthenticationRunner runner,
        IAuthenticationFlowProvider flows,
        ISessionCookieDelivery sessions,
        PresentedPasswordCredentialAccessor presented)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _presented = presented ?? throw new ArgumentNullException(nameof(presented));
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(PasswordLoginRequest req, CancellationToken ct)
    {
        // The caller names the flow, as a redirect sign-in does (FDW-903): which flow runs decides
        // whether a bearer token or a session comes back, and there is no default to fall back on.
        if (string.IsNullOrWhiteSpace(req.FlowName))
        {
            await Send.ResponseAsync(new PasswordLoginResponse(), StatusCodes.Status400BadRequest, ct)
                .ConfigureAwait(false);
            return;
        }

        // Not 401: the caller's credentials were never reached, and reporting a flow that is missing,
        // switched off or broken as a rejected password would send whoever reads it looking at the
        // wrong thing. Which of the three is FlowSelectionStatus's one mapping (FDW-910).
        var flow = await _flows.Get(req.FlowName, ct).ConfigureAwait(false);
        if (flow.IsFailure || flow.Value is null)
        {
            await Send.ResponseAsync(new PasswordLoginResponse(), FlowSelectionStatus.For(flow), ct)
                .ConfigureAwait(false);
            return;
        }

        // Presented to the step for exactly this run rather than put on the flow: a step's signature is
        // fixed by the pipeline, and putting a credential on the authentication context would make it
        // readable by every later step and printable by anything that logs the context.
        IGenericResult<FlowResult> result;
        using (new PresentedPasswordCredentialScope(_presented, req.Username, req.Password))
        {
            result = await _runner.Run(flow.Value, ct).ConfigureAwait(false);
        }

        // A password flow completes or it does not. Suspended and Waiting belong to flows that send
        // a caller elsewhere and expect them back; reaching one here means the flow was configured
        // with a step this endpoint cannot serve, which is a failure rather than a login to resume.
        // The password was proved but has to be changed first: said so, distinctly, so the caller can
        // send the user to change it rather than reading it as a wrong password (FDW-825).
        if (result.IsFailure && result.CodeChain.Contains(UserResultCodes.ByName("PasswordChangeRequired")))
        {
            await Send.ResponseAsync(new PasswordLoginResponse { PasswordChangeRequired = true }, StatusCodes.Status403Forbidden, ct)
                .ConfigureAwait(false);
            return;
        }

        if (result.IsFailure || result.Value is not FlowResult.Completed completed)
        {
            await Send.ResponseAsync(new PasswordLoginResponse(), FlowRunStatus.For(result), ct)
                .ConfigureAwait(false);
            return;
        }

        // A session-delivered flow's product is a session id. It goes into the HttpOnly cookie and
        // never into the body: a body is readable by the script the cookie exists to keep it from.
        if (flow.Value.SessionServiceName is not null)
        {
            var delivered = await _sessions.Deliver(HttpContext, flow.Value, completed.Token, ct).ConfigureAwait(false);
            if (delivered.IsFailure)
            {
                await Send.ResponseAsync(new PasswordLoginResponse(), StatusCodes.Status500InternalServerError, ct)
                    .ConfigureAwait(false);
                return;
            }

            await Send.OkAsync(
                new PasswordLoginResponse { TokenType = completed.Token.TokenType, ExpiresAt = completed.Token.ExpiresAt },
                ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(
            new PasswordLoginResponse
            {
                AccessToken = completed.Token.AccessToken,
                TokenType = completed.Token.TokenType,
                ExpiresAt = completed.Token.ExpiresAt,
            },
            ct).ConfigureAwait(false);
    }
}
