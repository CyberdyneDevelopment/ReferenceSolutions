using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization;
using System.Collections.Generic;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Endpoints;
using Microsoft.Extensions.Options;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to update a role.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateRoleEndpoint : UpdateRoleEndpointBase
{
    /// <inheritdoc />
    public UpdateRoleEndpoint(
        RoleConfigurationProvider roleProvider)
        : base(roleProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to UpdateRoleEndpointBase.AdminPolicy ("settings/role:delete"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s => s.Summary = "Update a role");
        Tags("Roles");
    }
}
