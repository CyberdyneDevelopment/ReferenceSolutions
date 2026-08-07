using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints.Roles;

/// <summary>Closure for the set role permissions endpoint.</summary>
[ExcludeFromCodeCoverage]
public sealed class SetRolePermissionsEndpoint : SetRolePermissionsEndpointBase
{
    private readonly ILogger<SetRolePermissionsEndpoint> _logger;

    /// <inheritdoc />
    public SetRolePermissionsEndpoint(
        DefaultConfigurationProvider<RolePermissionConfiguration, RolePermissionConfigurationCommand> rolePermissionProvider,
        RoleConfigurationProvider roleProvider,
        IConfigurationGateway configurationGateway,
        ISystemRoleConfiguration systemRoleConfiguration,
        ILogger<SetRolePermissionsEndpoint> logger)
        : base(rolePermissionProvider, roleProvider, configurationGateway, systemRoleConfiguration)
    {
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
