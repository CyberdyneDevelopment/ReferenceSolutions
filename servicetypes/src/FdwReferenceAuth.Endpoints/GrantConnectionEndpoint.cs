using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: grant a tenant a document connection for a purpose (FDW-1581).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GrantConnectionEndpoint : GrantConnectionEndpointBase
{
    /// <inheritdoc />
    public GrantConnectionEndpoint(
        IConnectionGrants grants,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles,
        ILogger<GrantConnectionEndpoint> logger)
        : base(logger, grants, audit, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("tenants:write");
        Summary(s =>
        {
            s.Summary = "Grant a tenant a document connection for a purpose";
            s.Description = "Gives a tenant use of one document connection for a purpose, optionally limited to a folder. Platform administrators only.";
        });
        Tags("Tenants");
    }
}
