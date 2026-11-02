using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: list the roles of an organization (FDW-1593).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListOrganizationRolesEndpoint : ListOrganizationRolesEndpointBase
{
    /// <inheritdoc />
    public ListOrganizationRolesEndpoint(
        IOrganizationRoles roles,
        ILogger<ListOrganizationRolesEndpoint> logger)
        : base(logger, roles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:read");
        Summary(s =>
        {
            s.Summary = "List the roles of an organization";
            s.Description = "Lists the roles the organization defines for its members. The caller must act in the organization.";
        });
        Tags("Organizations");
    }
}
