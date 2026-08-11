using System.Collections.Generic;
using Fdw.Services.Authorization;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Endpoints;
using Microsoft.Extensions.Options;

namespace ReferenceRoles.Endpoints;

/// <summary>
/// Closure for the list permissions endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListPermissionsEndpoint : ListPermissionsEndpointBase
{
    /// <inheritdoc />
    public ListPermissionsEndpoint(RoleConfigurationProvider roleProvider)
        : base(roleProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to ListPermissionsEndpointBase.ReadPolicy ("settings/role:read"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s => s.Summary = "List all permissions");
        Tags("Roles");
    }

    // Why: a handful of seed permissions use "/" in the resource (e.g. "notifications/lists:delete").
    // The API contract Newman exercises expects every name to match `^fdw:[a-z0-9_-]+:[a-z0-9_-]+$`,
    // so collapse "/" to "-" before serializing.
    protected override PermissionSummaryDto MapToSummary(PermissionConfiguration permission, string? orgPrefix)
    {
        var dto = base.MapToSummary(permission, orgPrefix);
        if (dto.Name.Contains('/'))
        {
            dto.Name = dto.Name.Replace('/', '-');
        }
        return dto;
    }
}
