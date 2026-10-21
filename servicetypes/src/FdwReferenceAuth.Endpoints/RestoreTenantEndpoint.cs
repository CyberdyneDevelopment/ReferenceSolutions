using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: restore a deleted company (FDW-1759).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RestoreTenantEndpoint : RestoreTenantEndpointBase
{
    /// <inheritdoc />
    public RestoreTenantEndpoint(
        ITenantWriter writer,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles,
        ILogger<RestoreTenantEndpoint> logger)
        : base(logger, writer, audit, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("tenants:write");
        Summary(s =>
        {
            s.Summary = "Restore a deleted company";
            s.Description = "Clears the deleted mark of a company. It comes back disabled; enabling it is a separate change. Only a platform administrator may.";
        });
        Tags("Tenants");
    }
}
