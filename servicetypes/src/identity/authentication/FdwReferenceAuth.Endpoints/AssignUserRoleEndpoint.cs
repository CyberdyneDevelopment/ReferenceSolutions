using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Users;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to assign a role to a user.
/// </summary>
[ExcludeFromCodeCoverage]
public class AssignUserRoleEndpoint : AssignUserRoleEndpointBase
{
    /// <inheritdoc />
    public AssignUserRoleEndpoint(IRoleConfigurationProvider roleProvider, UserRoleConfigurationProvider userRoleProvider, UserConfigurationProvider userProvider, ILogger<AssignUserRoleEndpoint> logger)
        : base(logger, roleProvider, userRoleProvider, userProvider) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s => s.Summary = "Assign a role to a user");
        Tags("Users");
    }
}
