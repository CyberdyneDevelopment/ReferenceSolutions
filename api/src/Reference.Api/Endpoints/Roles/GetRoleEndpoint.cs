using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to get a role by name with its permissions.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetRoleEndpoint : GetRoleEndpointBase
{
    /// <inheritdoc />
    public GetRoleEndpoint(RoleConfigurationProvider roleProvider)
        : base(roleProvider)
    {
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
