using System.Collections.Generic;
using Fdw.Services.Authorization;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Endpoints;
using Microsoft.Extensions.Options;

namespace Reference.Api.Endpoints.Roles;

/// <summary>
/// Closure for the list permissions grouped by resource endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListPermissionsGroupedEndpoint : ListPermissionsGroupedEndpointBase
{
    /// <inheritdoc />
    public ListPermissionsGroupedEndpoint(RoleConfigurationProvider roleProvider)
        : base(roleProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to ListPermissionsGroupedEndpointBase.ReadPolicy ("settings/role:read"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s => s.Summary = "List permissions grouped by resource");
        Tags("Roles");
    }
}
