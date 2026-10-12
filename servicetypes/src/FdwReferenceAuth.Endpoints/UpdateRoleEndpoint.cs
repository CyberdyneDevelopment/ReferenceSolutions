using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Endpoints;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to update a role.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateRoleEndpoint : UpdateRoleEndpointBase
{
    /// <inheritdoc />
    public UpdateRoleEndpoint(IRoleConfigurationProvider roleProvider, ILogger<UpdateRoleEndpoint> logger)
        : base(logger, roleProvider) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to UpdateRoleEndpointBase.AdminPolicy ("settings/role:delete"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s => s.Summary = "Update a role");
        Tags("Roles");
    }
}
