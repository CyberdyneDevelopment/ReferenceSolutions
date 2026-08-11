using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Endpoints;

namespace ReferenceRoles.Endpoints;

/// <summary>
/// Endpoint to list all roles.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListRolesEndpoint : ListRolesEndpointBase
{
    /// <inheritdoc />
    public ListRolesEndpoint(IAuthorizationProvider authorizationProvider)
        : base(authorizationProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to ListRolesEndpointBase.ReadPolicy ("settings/role:read"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s => s.Summary = "List all roles");
        Tags("Roles");
    }
}
