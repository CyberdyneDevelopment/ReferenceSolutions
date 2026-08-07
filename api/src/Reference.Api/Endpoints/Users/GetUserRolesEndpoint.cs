using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Users;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to get roles assigned to a user.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetUserRolesEndpoint : GetUserRolesEndpointBase
{
    /// <inheritdoc />
    public GetUserRolesEndpoint(
        RoleConfigurationProvider roleProvider,
        UserRoleConfigurationProvider userRoleProvider,
        UserConfigurationProvider userProvider)
        : base(roleProvider, userRoleProvider, userProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to GetUserRolesEndpointBase.ReadPolicy ("users:read"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s => s.Summary = "Get roles assigned to a user");
        Tags("Users");
    }
}
