using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Exchanges an already-issued foreign token (Entra, Auth0, Authentik, any OIDC provider) for one of
/// this host's own, via RFC 8693 token exchange.
/// </summary>
/// <remarks>
/// <para>
/// <c>ForeignTokenStepType</c> already does the verification and principal-resolution work — this
/// endpoint's only job is running the named flow. Unlike <see cref="OidcRedirectEndpoint"/>, the
/// <c>ForeignToken</c> step never suspends: the caller already holds the provider's token, so there is
/// no redirect, no resume cookie, and no callback. One request, one response.
/// </para>
/// <para>
/// There is no live <c>/connect/token</c> in this codebase — the OpenIddict-era generic,
/// <c>grant_type</c>-branching token endpoint was deleted, and nothing replaced it as a generic route.
/// This is a purpose-built endpoint for one flow shape, matching <see cref="PasswordLoginEndpoint"/>
/// and the OIDC redirect/callback pair rather than a bespoke grant on a generic token endpoint.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class TokenExchangeEndpoint : Endpoint<TokenExchangeRequest, PasswordLoginResponse>
{
    private readonly AuthenticationRunner _runner;
    private readonly IAuthenticationFlowProvider _flows;

    /// <summary>Initializes a new instance of the <see cref="TokenExchangeEndpoint"/> class.</summary>
    /// <param name="runner">Runs the flow.</param>
    /// <param name="flows">Supplies the flow to run.</param>
    public TokenExchangeEndpoint(AuthenticationRunner runner, IAuthenticationFlowProvider flows)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/auth/token-exchange");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(TokenExchangeRequest req, CancellationToken ct)
    {
        var flow = await _flows.Get(req.FlowName, ct).ConfigureAwait(false);
        if (flow.IsFailure || flow.Value is null)
        {
            // 404 and not 401: the caller named a flow that either doesn't exist or isn't wired —
            // a token that was never presented is a different failure than one that failed
            // verification, and conflating them sends an operator looking at the wrong thing.
            await Send.ResponseAsync(new PasswordLoginResponse(), StatusCodes.Status404NotFound, ct)
                .ConfigureAwait(false);
            return;
        }

        var result = await _runner.Run(flow.Value, ct).ConfigureAwait(false);

        // ForeignToken never suspends — Completed or it failed outright. Suspended/Waiting here would
        // mean the flow was configured with a step this endpoint cannot serve.
        if (result.IsFailure || result.Value is not FlowResult.Completed completed)
        {
            await Send.ResponseAsync(new PasswordLoginResponse(), FlowRunStatus.For(result), ct)
                .ConfigureAwait(false);
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
