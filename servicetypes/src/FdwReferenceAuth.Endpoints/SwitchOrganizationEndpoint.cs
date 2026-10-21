using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authentication.Sessions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Switches the caller to another department of the company they act in, by issuing them a new token that carries it (FDW-1802).
/// </summary>
/// <remarks>
/// The token comes from the same <c>TenantSwitch</c> authentication flow as a switch of company, with the department stated: the
/// caller is taken from the token this request was authenticated with, and issuance eligibility refuses anyone who may not act in the
/// department, a department that is disabled and a department of another company.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class SwitchOrganizationEndpoint : SwitchOrganizationEndpointBase
{
    private readonly SwitchFlowRunner _switch;

    /// <summary>Initializes a new instance of the <see cref="SwitchOrganizationEndpoint"/> class.</summary>
    public SwitchOrganizationEndpoint(
        IOrganizationProvider organizations,
        ITenantProvider tenants,
        IAdministrativeAudit audit,
        AuthenticationRunner runner,
        IAuthenticationFlowProvider flows,
        ISessionCookieDelivery sessions,
        ILogger<SwitchOrganizationEndpoint> logger)
        : base(logger, organizations, tenants, audit)
    {
        _switch = new SwitchFlowRunner(runner, flows, sessions, logger);
    }

    /// <inheritdoc />
    /// <remarks>Any authenticated caller may ask; whether they may act in the department is the flow's answer.</remarks>
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Act in another department";
            s.Description = "Issues a new token for the company the caller acts in that also carries the department they name. The caller must hold access in the department; the switch is audited.";
        });
        Tags("Organizations");
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<(string AccessToken, DateTimeOffset ExpiresAt)>> GenerateNewToken(
        Guid userId, Guid tenantId, OrganizationConfiguration organization, CancellationToken cancellationToken)
        => _switch.Run(HttpContext, userId.ToString(), tenantId, organization.Id, cancellationToken);
}
