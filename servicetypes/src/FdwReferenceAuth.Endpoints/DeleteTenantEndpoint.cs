using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: delete a company (FDW-1759).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteTenantEndpoint : DeleteTenantEndpointBase
{
    /// <inheritdoc />
    public DeleteTenantEndpoint(
        ITenantWriter writer,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles,
        ILogger<DeleteTenantEndpoint> logger)
        : base(logger, writer, audit, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("tenants:write");
        Summary(s =>
        {
            s.Summary = "Delete a company";
            s.Description = "Marks a disabled company deleted: it leaves every list and cannot be signed in to, but nothing it holds is removed, so it can be restored. Never the global company. Only a platform administrator may.";
        });
        Tags("Tenants");
    }
}
