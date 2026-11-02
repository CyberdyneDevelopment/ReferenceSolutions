using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Endpoints;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to list every user's role assignments in one read.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListAllUserRolesEndpoint : ListAllUserRolesEndpointBase
{
    /// <inheritdoc />
    public ListAllUserRolesEndpoint(
        IRoleConfigurationProvider roleProvider,
        UserRoleConfigurationProvider userRoleProvider,
        ILogger<ListAllUserRolesEndpoint> logger)
        : base(logger, roleProvider, userRoleProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy comes from ListAllUserRolesEndpointBase.ReadPolicy ("users:read"), applied by
        // the base's Configure(). Re-declaring Policies() here would AND a second requirement onto
        // it, not replace it.
        Summary(s =>
        {
            s.Summary = "List every user's role assignments";
            s.Description = "One read for a whole page of users. The users list does not carry roles.";
        });
        Tags("Users");
    }
}
