using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users;

namespace ReferenceUsers.Endpoints;

/// <summary>
/// Endpoint to assign a role to a user.
/// </summary>
[ExcludeFromCodeCoverage]
public class AssignUserRoleEndpoint : AssignUserRoleEndpointBase
{
    /// <inheritdoc />
    public AssignUserRoleEndpoint(
        RoleConfigurationProvider roleProvider,
        UserRoleConfigurationProvider userRoleProvider,
        IConfigurationGateway configurationGateway,
        UserConfigurationProvider userProvider)
        : base(roleProvider, userRoleProvider, configurationGateway, userProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s => s.Summary = "Assign a role to a user");
        Tags("Users");
    }
}
