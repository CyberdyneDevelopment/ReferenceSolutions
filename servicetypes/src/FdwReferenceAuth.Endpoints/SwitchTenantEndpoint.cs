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
    private readonly SwitchFlowRunner _switch;

    /// <summary>Initializes a new instance of the <see cref="SwitchTenantEndpoint"/> class.</summary>
    public SwitchTenantEndpoint(
        ITenantProvider tenantProvider,
        AuthenticationRunner runner,
        IAuthenticationFlowProvider flows,
        ISessionCookieDelivery sessions,
        ILogger<SwitchTenantEndpoint> logger)
        : base(logger, tenantProvider)
    {
        _switch = new SwitchFlowRunner(runner, flows, sessions, logger);
    }

    /// <inheritdoc />
    /// <remarks>Any authenticated caller may ask; whether they may act there is the flow's answer.</remarks>
    protected override void ConfigureEndpoint()
    {
        Tags("Tenants");
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<(string AccessToken, DateTimeOffset ExpiresAt)>> GenerateNewToken(
        string userId,
        ITenant tenant,
        CancellationToken ct)
        => _switch.Run(HttpContext, userId, tenant.Id, null, ct);

    /// <inheritdoc />
    protected override void OnTenantSwitched(string userId, Guid tenantId)
        => TenantLog.TenantSwitched(EndpointLogger, userId, tenantId);
}
