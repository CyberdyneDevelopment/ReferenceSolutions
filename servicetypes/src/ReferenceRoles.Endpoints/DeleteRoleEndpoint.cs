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
/// Endpoint to delete a role.
/// </summary>
[ExcludeFromCodeCoverage]
public class DeleteRoleEndpoint : DeleteRoleEndpointBase
{
    private readonly ILogger<DeleteRoleEndpoint> _logger;

    /// <inheritdoc />
    public DeleteRoleEndpoint(
        RoleConfigurationProvider roleProvider,
        ILogger<DeleteRoleEndpoint> logger)
        : base(roleProvider)
    {
        _logger = logger ?? NullLogger<DeleteRoleEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to DeleteRoleEndpointBase.DeletePolicy ("settings/role:delete"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s => s.Summary = "Delete a role");
        Tags("Roles");
    }

    /// <inheritdoc />
    protected override void OnDeletingRole(string roleName, Guid roleId)
    {
        RoleLog.DeletingRole(_logger, roleName, roleId);
    }

    /// <inheritdoc />
    protected override void OnRoleDeleted(string roleName)
    {
        RoleLog.RoleDeleted(_logger, roleName);
    }
}
