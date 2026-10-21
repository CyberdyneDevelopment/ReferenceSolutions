using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: take a document connection back from a tenant (FDW-1581).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RevokeConnectionGrantEndpoint : RevokeConnectionGrantEndpointBase
{
    /// <inheritdoc />
    public RevokeConnectionGrantEndpoint(
        IConnectionGrants grants,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles,
        ILogger<RevokeConnectionGrantEndpoint> logger)
        : base(logger, grants, audit, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("tenants:write");
        Summary(s =>
        {
            s.Summary = "Take a document connection back from a tenant";
            s.Description = "Removes a tenant's grant of a connection for a purpose. Refused while an organization is assigned to it. Platform administrators only.";
        });
        Tags("Tenants");
    }
}
