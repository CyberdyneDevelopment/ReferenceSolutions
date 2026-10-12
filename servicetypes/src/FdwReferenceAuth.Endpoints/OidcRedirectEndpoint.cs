using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceAuth.Endpoints.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>The flow to start, named by the caller rather than compiled in.</summary>
[ExcludeFromCodeCoverage]
public sealed class OidcRedirectRequest
{
    /// <summary>The <c>auth.AuthenticationFlow.Name</c> to run — e.g. "AuthentikLogin".</summary>
    public required string FlowName { get; set; }

    /// <summary>
    /// Where the browser is sent back to, with a one-time code, once the provider returns. Must be one
    /// the flow allows (<see cref="AuthenticationFlow.AllowsReturnUrl"/>: one of its <c>ReturnUrls</c>
    /// exactly, but a trailing slash is not significant); anything else is refused before the provider
    /// is ever contacted.
    /// </summary>
    public required string ReturnUrl { get; set; }
}

/// <summary>Where the caller's own browser must navigate to reach the provider.</summary>
[ExcludeFromCodeCoverage]
public sealed class OidcRedirectResponse
{
    /// <summary>The provider URL the caller must send the browser to, top-level (not fetched).</summary>
    public string RedirectUrl { get; set; } = string.Empty;
}

/// <summary>
/// Starts a login through an external OIDC provider, returning where the caller must send the
/// browser next.
/// </summary>
/// <remarks>
/// The endpoint runs a flow exactly like <see cref="PasswordLoginEndpoint"/> does — which steps that
/// flow has is configuration, not this file, and which flow to run is named by the caller in
/// <see cref="OidcRedirectRequest.FlowName"/> rather than hardcoded, so one endpoint pair serves every
/// external-IdP flow a host configures (Authentik, a second provider, whatever a UI adds a button
/// for) without a new endpoint class per provider.
/// <para>
/// This is a POST returning JSON rather than an HTTP redirect: the caller is a UI action (a "Sign in
/// with X" button), not a link, and the provider URL has to be a real top-level navigation the caller
/// performs itself — a fetch-followed redirect would try to load the provider's login page inside the
/// fetch response instead of the browser window, which fails for a cross-origin IdP.
/// </para>
/// Unlike password login, this flow's first step always suspends: <c>OidcRedirect</c> has nothing to
/// prove yet on the first call, so it always returns a challenge. The resume token the runner hands
/// back travels in a short-lived cookie rather than a query parameter — the OIDC callback's own query
/// string is reserved for the provider's own <c>code</c>/<c>state</c>, and a resume token riding
/// alongside them would be exposed in server logs and browser history for no benefit.
/// </remarks>
public sealed class OidcRedirectEndpoint : Endpoint<OidcRedirectRequest, OidcRedirectResponse>
{
    /// <summary>The cookie carrying the resume token across the redirect round-trip.</summary>
    internal const string ResumeCookieName = "fdw_oidc_resume";

    /// <summary>The cookie carrying the validated return URL across the redirect round-trip.</summary>
    internal const string ReturnCookieName = "fdw_oidc_return";

    /// <summary>The cookie carrying the flow name, whose key encrypts the one-time code's token.</summary>
    internal const string FlowCookieName = "fdw_oidc_flow";

    private readonly AuthenticationRunner _runner;
    private readonly IAuthenticationFlowProvider _flows;
    private readonly ILogger<OidcRedirectEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="OidcRedirectEndpoint"/> class.</summary>
    /// <param name="runner">Runs the flow.</param>
    /// <param name="flows">Supplies the flow to run.</param>
    /// <param name="logger">The logger.</param>
    public OidcRedirectEndpoint(
        AuthenticationRunner runner, IAuthenticationFlowProvider flows, ILogger<OidcRedirectEndpoint>? logger = null)
    {
        _logger = logger ?? NullLogger<OidcRedirectEndpoint>.Instance;
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/auth/oidc/redirect");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(OidcRedirectRequest req, CancellationToken ct)
    {
        // Missing, switched off or broken each answer differently; FlowSelectionStatus is the one mapping.
        var flow = await _flows.Get(req.FlowName, ct).ConfigureAwait(false);
        if (flow.IsFailure || flow.Value is null)
        {
            await Send.ResponseAsync(new OidcRedirectResponse(), FlowSelectionStatus.For(flow), ct)
                .ConfigureAwait(false);
            return;
        }

        // Checked before the provider is contacted: a return URL off the allow-list would turn the
        // callback into an open redirect that carries a sign-in code. A flow with no ReturnUrls never
        // returns a browser, so it is refused the same way rather than sent somewhere assumed.
        if (!flow.Value.AllowsReturnUrl(req.ReturnUrl))
        {
            AuthenticationLog.ReturnUrlNotAllowed(_logger, req.FlowName);
            await Send.ResponseAsync(new OidcRedirectResponse(), StatusCodes.Status400BadRequest, ct)
                .ConfigureAwait(false);
            return;
        }

        var result = await _runner.Run(flow.Value, ct).ConfigureAwait(false);

        // The first step of this flow (OidcRedirect) always challenges on a fresh run — it has no
        // authorization code yet. Anything other than Suspended here means the flow is misconfigured
        // (a step failed outright, or completed without ever reaching the redirect), not a login to
        // finish.
        if (result.IsFailure || result.Value is not FlowResult.Suspended suspended)
        {
            await Send.ResponseAsync(new OidcRedirectResponse(), StatusCodes.Status500InternalServerError, ct)
                .ConfigureAwait(false);
            return;
        }

        var cookie = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            // Lax, not Strict: this cookie must still be sent on the top-level GET redirect
            // Authentik issues back to /auth/oidc/callback, which is a cross-site navigation from
            // the cookie's own origin's point of view.
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(flow.Value.ExecutionLifetime),
        };
        HttpContext.Response.Cookies.Append(ResumeCookieName, suspended.ResumeToken, cookie);
        HttpContext.Response.Cookies.Append(ReturnCookieName, req.ReturnUrl, cookie);
        HttpContext.Response.Cookies.Append(FlowCookieName, req.FlowName, cookie);

        // AbsoluteUri, not ToString(): ToString() undoes the percent-encoding the step gave the query, so the
        // scope would reach the browser with raw spaces (FDW-1037).
        await Send.OkAsync(new OidcRedirectResponse { RedirectUrl = suspended.RedirectTo.AbsoluteUri }, ct)
            .ConfigureAwait(false);
    }
}
