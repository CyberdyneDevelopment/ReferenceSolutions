using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: delete a role of an organization (FDW-1593).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteOrganizationRoleEndpoint : DeleteOrganizationRoleEndpointBase
{
    /// <inheritdoc />
    public DeleteOrganizationRoleEndpoint(
        IOrganizationRoles roles,
        IAdministrativeAudit audit,
        ILogger<DeleteOrganizationRoleEndpoint> logger)
        : base(logger, roles, audit)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:write");
        Summary(s =>
        {
            s.Summary = "Delete a role of an organization";
            s.Description = "Deletes a role nobody holds and the organization was not made with.";
        });
        Tags("Organizations");
    }
}
