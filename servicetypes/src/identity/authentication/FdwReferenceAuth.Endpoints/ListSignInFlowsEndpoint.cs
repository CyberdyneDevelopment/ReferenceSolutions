using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Users.Endpoints;
using FdwReferenceAuth.Endpoints.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Lists the sign-in flows a page may offer: <c>GET /auth/sign-in-flows?returnUrl=</c>.
/// </summary>
/// <remarks>
/// <para>
/// A page is offered only the flows whose own configuration says they can sign it in, so no page
/// keeps a list of flow names (FDW-919):
/// </para>
/// <list type="bullet">
/// <item>An <c>external</c> flow - one with a federating step - when its <c>ReturnUrls</c> allow the
/// page's URL (<see cref="AuthenticationFlow.AllowsReturnUrl"/>: exact, but a trailing slash is not
/// significant), the same check <c>/auth/oidc/redirect</c> makes before it sends a browser to the
/// provider.</item>
/// <item>A <c>password</c> flow - one with a step that proves <c>pwd</c> - when it delivers into a
/// session service whose allowed origins include the page's origin. A password flow that issues a
/// bearer token is never offered: a browser page cannot hold one.</item>
/// </list>
/// <para>
/// Anonymous, like the provider list: it is what a signed-out page asks. It says less than
/// <c>/auth/flows</c> - no steps, no permissions, no return URLs - only the name to post back, what to
/// label it, and which kind of control to draw. A flow whose provider or session service cannot be
/// read fails the whole request rather than dropping out, so an empty list means exactly that.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ListSignInFlowsEndpoint : Endpoint<ListSignInFlowsRequest, SignInFlowListResponse>
{
    private const string ExternalKind = "external";
    private const string PasswordKind = "password";

    private readonly IAuthenticationFlowProvider _flows;
    private readonly IAuthenticationServiceConfigurationProvider _services;
    private readonly ILogger _log;

    /// <summary>Initializes a new instance of the <see cref="ListSignInFlowsEndpoint"/> class.</summary>
    /// <param name="flows">Supplies the flows this host serves.</param>
    /// <param name="services">Supplies the session services password flows deliver into.</param>
    /// <param name="logger">The logger.</param>
    public ListSignInFlowsEndpoint(
        IAuthenticationFlowProvider flows,
        IAuthenticationServiceConfigurationProvider services,
        ILogger<ListSignInFlowsEndpoint>? logger = null)
    {
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = logger ?? NullLogger<ListSignInFlowsEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/auth/sign-in-flows");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List the sign-in flows a page may offer";
            s.Description = "The flows that can sign a browser in on the page at returnUrl: external flows whose "
                + "return URLs list it, and password flows whose session service accepts its origin. Each has the "
                + "name to send back as flowName, a label, and its kind: external or password.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ListSignInFlowsRequest req, CancellationToken ct)
    {
        AuthenticationLog.ListingSignInFlows(_log, req.ReturnUrl);

        if (!Uri.TryCreate(req.ReturnUrl, UriKind.Absolute, out var returnUri))
        {
            AuthenticationLog.SignInFlowsReturnUrlInvalid(_log, req.ReturnUrl);
            await Send.ResponseAsync(new SignInFlowListResponse(), StatusCodes.Status400BadRequest, ct).ConfigureAwait(false);
            return;
        }

        var flows = await _flows.Get(ct).ConfigureAwait(false);
        if (flows.IsFailure || flows.Value is not { } configured)
        {
            AuthenticationLog.SignInFlowsUnreadable(_log);
            await Send.ResponseAsync(new SignInFlowListResponse(), StatusCodes.Status500InternalServerError, ct).ConfigureAwait(false);
            return;
        }

        var origin = returnUri.GetLeftPart(UriPartial.Authority);
        var items = new List<SignInFlowDto>();
        foreach (var flow in configured)
        {
            var offered = await Offer(flow, req.ReturnUrl, origin, ct).ConfigureAwait(false);
            if (offered.IsFailure)
            {
                await Send.ResponseAsync(new SignInFlowListResponse(), StatusCodes.Status500InternalServerError, ct).ConfigureAwait(false);
                return;
            }

            if (offered.Value is { } item)
                items.Add(item);
        }

        AuthenticationLog.SignInFlowsListed(_log, items.Count, configured.Count, req.ReturnUrl);
        await Send.OkAsync(new SignInFlowListResponse { Items = items }, ct).ConfigureAwait(false);
    }

    private async Task<IGenericResult<SignInFlowDto?>> Offer(
        AuthenticationFlow flow, string returnUrl, string origin, CancellationToken ct)
    {
        foreach (var step in flow.Steps)
        {
            // The collection is the registry: a step's name resolves to the option that declared it,
            // and the option itself says whether it federates.
            if (AuthenticationStepTypes.ByName(step.Name) is IFederatingAuthenticationStep federating)
                return await OfferExternal(flow, step.Name, federating, returnUrl, ct).ConfigureAwait(false);
        }

        var provesPassword = flow.Steps
            .Select(step => AuthenticationStepTypes.ByName(step.Name))
            .OfType<IAuthenticationStep>()
            .Any(step => step.AuthenticationMethods.Contains(PasswordAdmission.Method, StringComparer.Ordinal));

        if (provesPassword)
            return await OfferPassword(flow, returnUrl, origin, ct).ConfigureAwait(false);

        return NotOffered(flow, returnUrl, "it neither federates nor proves a password");
    }

    private async Task<IGenericResult<SignInFlowDto?>> OfferExternal(
        AuthenticationFlow flow, string stepName, IFederatingAuthenticationStep federating, string returnUrl, CancellationToken ct)
    {
        if (!flow.AllowsReturnUrl(returnUrl))
            return NotOffered(flow, returnUrl, "its ReturnUrls do not list this page");

        var authority = await federating.GetAuthorityName(ct).ConfigureAwait(false);
        if (authority.IsFailure || string.IsNullOrWhiteSpace(authority.Value))
        {
            return GenericResult<SignInFlowDto?>.Failure(AuthenticationLog.SignInFlowAuthorityUnreadable(
                _log, flow.Name, stepName, authority.IsFailure ? "the step could not read it" : "the step returned no name"));
        }

        return GenericResult<SignInFlowDto?>.Success(
            new SignInFlowDto { Name = flow.Name, DisplayName = authority.Value, Kind = ExternalKind });
    }

    private async Task<IGenericResult<SignInFlowDto?>> OfferPassword(
        AuthenticationFlow flow, string returnUrl, string origin, CancellationToken ct)
    {
        if (flow.SessionServiceName is not { } serviceName)
            return NotOffered(flow, returnUrl, "it issues a bearer token, which a browser page cannot hold");

        var service = await _services.Get(serviceName, ct).ConfigureAwait(false);
        if (service.IsFailure || service.Value is not ISessionAuthenticationConfiguration session)
        {
            return GenericResult<SignInFlowDto?>.Failure(AuthenticationLog.SignInFlowSessionServiceUnreadable(
                _log, flow.Name, serviceName,
                service.IsFailure ? "the service could not be read" : "the service is not a Session implementation"));
        }

        // Split the way OriginCheck splits it, so the list offers exactly what the session will accept.
        var accepted = session.AllowedOrigins is { } allowedOrigins
            && allowedOrigins.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Contains(origin, StringComparer.OrdinalIgnoreCase);

        return accepted
            ? GenericResult<SignInFlowDto?>.Success(new SignInFlowDto { Name = flow.Name, DisplayName = flow.Name, Kind = PasswordKind })
            : NotOffered(flow, returnUrl, "its session service '" + serviceName + "' does not accept this page's origin");
    }

    private IGenericResult<SignInFlowDto?> NotOffered(AuthenticationFlow flow, string returnUrl, string reason)
    {
        AuthenticationLog.SignInFlowNotOffered(_log, flow.Name, returnUrl, reason);
        return GenericResult<SignInFlowDto?>.Success(null);
    }
}
