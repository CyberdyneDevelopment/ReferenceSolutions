using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authentication.Sessions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;
using FdwReferenceAuth.Endpoints.Logging;
using FdwReferenceAuthentication.TenantSwitch.Registration;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Switches the caller to another tenant they may act in, by issuing them a new token for it.
/// </summary>
/// <remarks>
/// <para>
/// A user acts in one tenant at a time — the tenant their token was issued for — and this is the
/// only way to change it (FDW-825). A member of several tenants switches between them; a member of
/// the global tenant may switch into any tenant and operate as it.
/// </para>
/// <para>
/// The token comes from the <c>TenantSwitch</c> authentication flow, not a hand-built one: the
/// caller is taken from the token this request was authenticated with, the new token carries only
/// the roles held in the target tenant, and issuance eligibility refuses anyone who may not act
/// there — the same checks every token passes.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class SwitchTenantEndpoint : SwitchTenantEndpointBase
{
    private const string FlowName = "TenantSwitch";

    // A caller signed in by a server-side session stays one after switching (FDW-903): its switch runs
    // the session-delivered twin, so no bearer token is ever handed to the browser app.
    private const string SessionFlowName = "TenantSwitchSession";

    private readonly AuthenticationRunner _runner;
    private readonly IAuthenticationFlowProvider _flows;
    private readonly ISessionCookieDelivery _sessions;

    /// <summary>Initializes a new instance of the <see cref="SwitchTenantEndpoint"/> class.</summary>
    public SwitchTenantEndpoint(
        ITenantProvider tenantProvider,
        AuthenticationRunner runner,
        IAuthenticationFlowProvider flows,
        ISessionCookieDelivery sessions,
        ILogger<SwitchTenantEndpoint> logger)
        : base(logger, tenantProvider)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    }

    /// <inheritdoc />
    /// <remarks>Any authenticated caller may ask; whether they may act there is the flow's answer.</remarks>
    protected override void ConfigureEndpoint()
    {
        Tags("Tenants");
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<(string AccessToken, DateTimeOffset ExpiresAt)>> GenerateNewToken(
        string userId,
        ITenant tenant,
        CancellationToken ct)
    {
        // Handed to the step through the request, the same way the password step receives its
        // credential: a step's signature is fixed by the pipeline.
        HttpContext.Items[HttpTenantSwitchRequestAccessor.RequestedTenantItemKey] = tenant.Id;

        var flowName = _sessions.IsSessionRequest(HttpContext) ? SessionFlowName : FlowName;
        var flow = await _flows.Get(flowName, ct).ConfigureAwait(false);
        if (flow.IsFailure || flow.Value is null)
        {
            return GenericResult<(string, DateTimeOffset)>.Failure(
                TenantLog.TenantSwitchNotIssued(EndpointLogger, userId, tenant.Id, $"no flow named '{flowName}' is configured"));
        }

        var result = await _runner.Run(flow.Value, ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return result.ToNewResult<(string, DateTimeOffset)>();
        }

        // TenantSwitch never suspends; reaching Suspended or Waiting means the flow was configured
        // with a step this endpoint cannot serve.
        if (result.Value is not FlowResult.Completed completed)
        {
            return GenericResult<(string, DateTimeOffset)>.Failure(
                TenantLog.TenantSwitchNotIssued(EndpointLogger, userId, tenant.Id, "the flow did not complete"));
        }

        if (flow.Value.SessionServiceName is null)
            return GenericResult<(string, DateTimeOffset)>.Success((completed.Token.AccessToken, completed.Token.ExpiresAt));

        var replaced = await _sessions.Replace(HttpContext, flow.Value, completed.Token, ct).ConfigureAwait(false);
        if (replaced.IsFailure)
            return replaced.ToNewResult<(string, DateTimeOffset)>();

        // The new session id is in the cookie Replace set. The response body carries no credential for
        // a session caller, so the access token it reports is empty by design, not by omission.
        return GenericResult<(string, DateTimeOffset)>.Success((string.Empty, completed.Token.ExpiresAt));
    }

    /// <inheritdoc />
    protected override void OnTenantSwitched(string userId, Guid tenantId)
        => TenantLog.TenantSwitched(EndpointLogger, userId, tenantId);
}
