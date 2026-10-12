using Microsoft.Extensions.Logging;
using Fdw.Services.Authorization;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization.Endpoints;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Closure for the list permissions endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListPermissionsEndpoint : ListPermissionsEndpointBase
{
    /// <inheritdoc />
    public ListPermissionsEndpoint(IPermissionConfigurationProvider permissionProvider, ILogger<ListPermissionsEndpoint> logger)
        : base(logger, permissionProvider, null) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to ListPermissionsEndpointBase.ReadPolicy ("settings/role:read"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s => s.Summary = "List all permissions");
        Tags("Roles");
    }

}
