using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceAuth.Endpoints.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>Closure for the set role permissions endpoint.</summary>
[ExcludeFromCodeCoverage]
public class SetRolePermissionsEndpoint : SetRolePermissionsEndpointBase
{
    private readonly ILogger<SetRolePermissionsEndpoint> _logger;

    /// <inheritdoc />
    public SetRolePermissionsEndpoint(
        IRolePermissionConfigurationProvider rolePermissionProvider,
        IRoleConfigurationProvider roleProvider,
        IPermissionConfigurationProvider permissionProvider,
        ISystemRoleConfiguration systemRoleConfiguration,
        IUserRoleConfigurationProvider userRoles,
        Fdw.Services.TokenManagers.Abstractions.ITokenRevocationStore revocations,
        ILogger<SetRolePermissionsEndpoint> logger)
        : base(logger, rolePermissionProvider, roleProvider, permissionProvider, systemRoleConfiguration, userRoles, revocations, null) {
        _logger = logger ?? NullLogger<SetRolePermissionsEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: the admin-only guard now lives on SetRolePermissionsEndpointBase.AdminPolicy
        // ("settings/role:delete", applied by the base's Configure()) — same permission as before,
        // declared once for every host. Override AdminPolicy to change the tier for this host.
        Summary(s => s.Summary = "Set role permissions");
        Tags("Roles");
    }

    /// <inheritdoc />
    protected override void OnPermissionUpdateFailed(string roleName)
    {
        RoleLog.RolePermissionsSetFailed(_logger, roleName, "Permission update failed");
    }
}
