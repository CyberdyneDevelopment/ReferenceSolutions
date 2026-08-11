using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Endpoints;

namespace ReferenceRoles.Endpoints;

/// <summary>
/// Closure for the get role permissions endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetRolePermissionsEndpoint : GetRolePermissionsEndpointBase
{
    /// <inheritdoc />
    public GetRolePermissionsEndpoint(RoleConfigurationProvider roleProvider)
        : base(roleProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to GetRolePermissionsEndpointBase.ReadPolicy ("settings/role:read"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s => s.Summary = "Get role permissions");
        Tags("Roles");
    }
}
