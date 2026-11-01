using System.Linq;
using System;
using System.Collections.Generic;
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

/// <summary>One external login this host offers.</summary>
[ExcludeFromCodeCoverage]
public sealed class ExternalIdentityProviderDto
{
    /// <summary>
    /// What to post back as <c>flowName</c> to start this login.
    /// </summary>
    /// <remarks>
    /// The flow's name, not the authority's. A caller acts on this by handing it straight to
    /// <see cref="OidcRedirectEndpoint"/>, so a label that reads better but cannot be posted back
    /// would be worse than no label at all.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>What to put on the button — the authority the flow sends the caller to.</summary>
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>The external logins on offer.</summary>
[ExcludeFromCodeCoverage]
public sealed class ExternalIdentityProviderListResponse
{
    /// <summary>Gets or sets the providers, empty when this host federates with nobody.</summary>
    public IReadOnlyList<ExternalIdentityProviderDto> Items { get; set; } = [];
}

/// <summary>
/// Lists the external logins this host serves, so a Login screen can draw a button per provider
/// instead of hardcoding a flow name or guessing whether SSO exists.
/// </summary>
/// <remarks>
/// <para>
/// Anonymous, because nobody holds a token before choosing how to log in — a gate here is a screen
/// nobody can get past.
/// </para>
/// <para>
/// A flow is on this list when one of its steps declares itself an
/// <see cref="IFederatingAuthenticationStep"/>. Matching the step's name instead would list exactly
/// the one federating step that ships today and silently omit the next one, which is the open set
/// <c>AuthenticationStepTypes</c> exists to keep open.
/// </para>
/// <para>
/// Nothing about the authority itself is returned but its name. The browser never contacts the
/// provider directly — it posts the flow name back and follows the <c>redirectUrl</c> the redirect
/// endpoint returns — so issuer and endpoint URLs would widen what an unauthenticated caller learns
/// for nothing the screen uses.
/// </para>
/// <para>
/// An authority that cannot be read fails the whole request rather than dropping that flow from the
/// list. A short list and a correct one are indistinguishable to the caller, and a caller who
/// believes nothing is configured tells the user their administrator has not set SSO up — which is
/// a worse thing to say than nothing. Failing means an empty list means exactly what it says.
/// </para>
/// </remarks>
public sealed class ListExternalIdentityProvidersEndpoint : EndpointWithoutRequest<ExternalIdentityProviderListResponse>
{
    private readonly IAuthenticationFlowProvider _flows;
    private readonly ILogger _log;

    /// <summary>Initializes a new instance of the <see cref="ListExternalIdentityProvidersEndpoint"/> class.</summary>
    /// <param name="flows">Supplies the flows this host serves.</param>
    /// <param name="logger">The logger.</param>
    public ListExternalIdentityProvidersEndpoint(
        IAuthenticationFlowProvider flows,
        ILogger<ListExternalIdentityProvidersEndpoint>? logger = null)
    {
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _log = logger ?? NullLogger<ListExternalIdentityProvidersEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/auth/external-identity-providers");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        AuthenticationLog.ListingExternalProviders(_log);

        var flows = await _flows.Get(ct).ConfigureAwait(false);
        if (flows.IsFailure || flows.Value is null)
        {
            AuthenticationLog.ExternalProviderFlowsUnreadable(_log, flows.CurrentMessage ?? "no flows returned");
            await Send.ResponseAsync(
                new ExternalIdentityProviderListResponse(), StatusCodes.Status500InternalServerError, ct)
                .ConfigureAwait(false);
            return;
        }

        var items = new List<ExternalIdentityProviderDto>();

        foreach (var flow in flows.Value)
        {
            foreach (var stepName in flow.Steps.Select(s => s.Name))
            {
                // The collection IS the registry: a flow's step name resolves to the option that
                // declared it, and the option itself says whether it federates.
                if (AuthenticationStepTypes.ByName(stepName) is not IFederatingAuthenticationStep federating)
                    continue;

                var authority = await federating.GetAuthorityName(ct).ConfigureAwait(false);
                if (authority.IsFailure || string.IsNullOrWhiteSpace(authority.Value))
                {
                    AuthenticationLog.ExternalProviderAuthorityUnreadable(
                        _log, flow.Name, stepName, authority.CurrentMessage ?? "no name returned");
                    await Send.ResponseAsync(
                        new ExternalIdentityProviderListResponse(), StatusCodes.Status500InternalServerError, ct)
                        .ConfigureAwait(false);
                    return;
                }

                items.Add(new ExternalIdentityProviderDto
                {
                    Name = flow.Name,
                    DisplayName = authority.Value,
                });

                // One button per flow. A flow with two federating steps chains providers rather than
                // offering a choice between them, so listing it twice would offer the caller a
                // decision the flow does not have.
                break;
            }
        }

        AuthenticationLog.ExternalProvidersListed(_log, items.Count, flows.Value.Count);

        await Send.OkAsync(new ExternalIdentityProviderListResponse { Items = items }, ct).ConfigureAwait(false);
    }
}
