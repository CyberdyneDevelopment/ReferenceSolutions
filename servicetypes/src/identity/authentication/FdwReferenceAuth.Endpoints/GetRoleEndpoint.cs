using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Endpoints;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to get a role by name with its permissions.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetRoleEndpoint : GetRoleEndpointBase
{
    /// <inheritdoc />
    public GetRoleEndpoint(
        IRoleConfigurationProvider roleProvider,
        IPermissionConfigurationProvider permissionProvider,
        IRolePermissionConfigurationProvider rolePermissionProvider,
        ILogger<GetRoleEndpoint> logger)
        : base(logger, roleProvider, permissionProvider, rolePermissionProvider) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to GetRoleEndpointBase.ReadPolicy ("settings/role:read"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s => s.Summary = "Get role by name");
        Tags("Roles");
    }
}
