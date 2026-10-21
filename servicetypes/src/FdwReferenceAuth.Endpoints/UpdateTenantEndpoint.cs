using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to rename, disable, enable or re-connect a tenant (FDW-1578).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateTenantEndpoint : UpdateTenantEndpointBase
{
    /// <inheritdoc />
    public UpdateTenantEndpoint(
        ITenantWriter tenantWriter,
        ITenantProvider tenantProvider,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoleConfiguration,
        ILogger<UpdateTenantEndpoint> logger)
        : base(logger, tenantWriter, tenantProvider, audit, systemRoleConfiguration)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("tenants:write");
        Summary(s =>
        {
            s.Summary = "Change a tenant";
            s.Description = "Renames, disables, enables or re-connects a tenant; a part left out keeps its value. Only a platform administrator may.";
        });
        Tags("Tenants");
    }
}
