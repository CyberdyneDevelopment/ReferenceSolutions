using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Execution;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceAuth.Endpoints.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Receives the OIDC provider's callback, resumes the suspended login, and sends the browser back to
/// the caller's return URL with a one-time code.
/// </summary>
/// <remarks>
/// <para>
/// The provider's own <c>code</c>/<c>state</c> query parameters are read directly off the request by
/// <c>HttpOidcCallbackAccessor</c>. This endpoint recovers what <see cref="OidcRedirectEndpoint"/>
/// stashed in cookies before the redirect: the resume token, the flow, and the validated return URL.
/// </para>
/// <para>
/// It answers with a redirect, not a token body. The browser arrives here by a top-level navigation
/// from the provider, so a different-origin console could never read a JSON body from this response.
/// The issued token goes into <see cref="IAuthorizationCodeStore"/> instead, and the browser returns to
/// <c>returnUrl?auth_code=...</c>; the console spends the code once at <c>POST /auth/redeem</c>. A sign-in
/// that fails returns the browser with <c>error</c>/<c>error_description</c>, the same parameters an
/// OAuth provider uses, so the console reports it on the page it already has.
/// </para>
/// <para>
/// Only when there is nowhere trusted to send the browser (the return cookies are gone, or no longer on
/// the flow's allow-list) does it answer 400 here.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class OidcCallbackEndpoint : EndpointWithoutRequest<PasswordLoginResponse>
{
    private readonly AuthenticationRunner _runner;
    private readonly IAuthenticationFlowProvider _flows;
    private readonly IAuthorizationCodeStore _codes;
    private readonly ILogger<OidcCallbackEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="OidcCallbackEndpoint"/> class.</summary>
    /// <param name="runner">Runs the flow.</param>
    /// <param name="flows">Supplies the flow, whose allow-list the return URL is checked against again.</param>
    /// <param name="codes">Holds the issued token behind a one-time code.</param>
    /// <param name="logger">The logger.</param>
    public OidcCallbackEndpoint(
        AuthenticationRunner runner,
        IAuthenticationFlowProvider flows,
        IAuthorizationCodeStore codes,
        ILogger<OidcCallbackEndpoint>? logger = null)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _logger = logger ?? NullLogger<OidcCallbackEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/auth/oidc/callback");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var cookies = HttpContext.Request.Cookies;
        var resumeToken = cookies[OidcRedirectEndpoint.ResumeCookieName];
        var returnUrl = cookies[OidcRedirectEndpoint.ReturnCookieName];
        var flowName = cookies[OidcRedirectEndpoint.FlowCookieName];

        var (received, carried) = Report(cookies);
        AuthenticationLog.CallbackCookiesReceived(_logger, received, HttpContext.Request.Scheme, HttpContext.Request.Host.ToString(), carried);

        HttpContext.Response.Cookies.Delete(OidcRedirectEndpoint.ResumeCookieName);
        HttpContext.Response.Cookies.Delete(OidcRedirectEndpoint.ReturnCookieName);
        HttpContext.Response.Cookies.Delete(OidcRedirectEndpoint.FlowCookieName);

        if (string.IsNullOrEmpty(returnUrl) || string.IsNullOrEmpty(flowName))
        {
            AuthenticationLog.CallbackReturnTargetMissing(_logger, received, HttpContext.Request.Scheme, HttpContext.Request.Host.ToString(), carried);
            await Send.ResponseAsync(new PasswordLoginResponse(), StatusCodes.Status400BadRequest, ct)
                .ConfigureAwait(false);
            return;
        }

        // Checked again, not trusted from the cookie: the allow-list may have changed while the
        // caller was away, and a redirect off it would carry a sign-in code to a stranger.
        var flow = await _flows.Get(flowName, ct).ConfigureAwait(false);
        if (flow.IsFailure || flow.Value is not { } allowedFlow || !allowedFlow.AllowsReturnUrl(returnUrl))
        {
            AuthenticationLog.ReturnUrlNotAllowed(_logger, flowName);
            await Send.ResponseAsync(new PasswordLoginResponse(), StatusCodes.Status400BadRequest, ct)
                .ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrEmpty(resumeToken))
        {
            // The resume cookie lives only as long as the flow's execution: the caller took too long
            // at the provider, or arrived here without starting a sign-in.
            AuthenticationLog.CallbackResumeMissing(_logger, flowName);
            await ReturnWith(returnUrl, "login_required", "The sign-in took too long. Start it again.")
                .ConfigureAwait(false);
            return;
        }

        var result = await _runner.Resume(resumeToken, ct).ConfigureAwait(false);

        // Completed is the only outcome a callback can finish. Suspended/Waiting would mean the flow
        // challenges more than once, which OidcRedirect never does; a failure means the code, state,
        // nonce, or signature did not check out, or the sign-in was accepted and this host could not
        // issue its session, which is the host's fault and not a denial (FDW-918).
        if (result.IsFailure || result.Value is not FlowResult.Completed completed)
        {
            // The runner and its steps already logged why; this records which flow's sign-in it was.
            AuthenticationLog.CallbackSignInFailed(_logger, flowName);
            await (FlowRunStatus.IsIssuanceFault(result)
                    ? ReturnWith(returnUrl, "server_error", "Signed in, but this server could not create the session.")
                    : ReturnWith(returnUrl, "access_denied", "The identity provider sign-in could not be completed."))
                .ConfigureAwait(false);
            return;
        }

        var code = await _codes.Issue(flowName, completed.Token, ct).ConfigureAwait(false);
        if (code.IsFailure || code.Value is null)
        {
            // The store already logged why.
            AuthenticationLog.CallbackCodeNotIssued(_logger, flowName);
            await ReturnWith(returnUrl, "server_error", "Signed in, but the sign-in could not be handed back to this page.")
                .ConfigureAwait(false);
            return;
        }

        AuthenticationLog.CallbackCompleted(_logger, flowName);
        await Send.RedirectAsync(Append(returnUrl, "auth_code", code.Value), isPermanent: false, allowRemoteRedirects: true)
            .ConfigureAwait(false);
    }

    private Task<FastEndpoints.Void> ReturnWith(string returnUrl, string error, string description)
        => Send.RedirectAsync(
            Append(Append(returnUrl, "error", error), "error_description", description),
            isPermanent: false,
            allowRemoteRedirects: true);

    // Which of the sign-in cookies arrived, and the names of every cookie the request carried: names only, never values.
    // What the browser sent is what tells a cookie it never kept from one it sent to the wrong place.
    private static (string Received, string Carried) Report(IRequestCookieCollection cookies)
    {
        var received = new[] { OidcRedirectEndpoint.ResumeCookieName, OidcRedirectEndpoint.ReturnCookieName, OidcRedirectEndpoint.FlowCookieName }
            .Where(name => !string.IsNullOrEmpty(cookies[name]))
            .ToList();
        return (received.Count == 0 ? "none" : string.Join(", ", received),
                cookies.Count == 0 ? "none" : string.Join(", ", cookies.Keys));
    }

    private static string Append(string url, string name, string value)
        => string.Concat(url, url.Contains('?', StringComparison.Ordinal) ? "&" : "?", name, "=", Uri.EscapeDataString(value));
}
