using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceRoles.Endpoints.Logging;

namespace ReferenceRoles.Endpoints;

/// <summary>
/// Endpoint to create a new role.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateRoleEndpoint : CreateRoleEndpointBase
{
    private readonly ILogger<CreateRoleEndpoint> _logger;

    /// <inheritdoc />
    public CreateRoleEndpoint(
        RoleConfigurationProvider roleProvider,
        ILogger<CreateRoleEndpoint> logger)
        : base(roleProvider)
    {
        _logger = logger ?? NullLogger<CreateRoleEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to CreateRoleEndpointBase.AdminPolicy ("settings/role:delete"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s => s.Summary = "Create a new role");
        Tags("Roles");
    }

    /// <inheritdoc />
    protected override void OnCreatingRole(string roleName)
    {
        RoleLog.CreatingRole(_logger, roleName);
    }

    /// <inheritdoc />
    protected override void OnRoleCreated(string roleName, Guid roleId)
    {
        RoleLog.RoleCreated(_logger, roleName, roleId);
    }
}
