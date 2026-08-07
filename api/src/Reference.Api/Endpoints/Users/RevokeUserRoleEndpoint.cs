using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to revoke a role from a user.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RevokeUserRoleEndpoint : RevokeUserRoleEndpointBase
{
    /// <inheritdoc />
    public RevokeUserRoleEndpoint(
        RoleConfigurationProvider roleProvider,
        UserRoleConfigurationProvider userRoleProvider,
        UserConfigurationProvider userProvider,
        IConfigurationGateway configurationGateway)
        : base(roleProvider, userRoleProvider, userProvider, configurationGateway)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: the admin-only guard now lives on RevokeUserRoleEndpointBase.AdminPolicy
        // ("users:delete", applied by the base's Configure()) so every host inherits it instead of
        // re-typing it per app. Declaring Policies() again here would AND a second requirement onto
        // the base's, not replace it. Override AdminPolicy to change the tier for this host.
        Summary(s => s.Summary = "Revoke a role from a user");
        Tags("Users");
    }
}
