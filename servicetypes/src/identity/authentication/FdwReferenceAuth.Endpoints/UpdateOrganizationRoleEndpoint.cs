using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: change a role of an organization (FDW-1593).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateOrganizationRoleEndpoint : UpdateOrganizationRoleEndpointBase
{
    /// <inheritdoc />
    public UpdateOrganizationRoleEndpoint(
        IOrganizationRoles roles,
        IAdministrativeAudit audit,
        ILogger<UpdateOrganizationRoleEndpoint> logger)
        : base(logger, roles, audit)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:write");
        Summary(s =>
        {
            s.Summary = "Change a role of an organization";
            s.Description = "Changes the description and permissions of a role; everyone who holds it is refreshed with the permissions it carries from now.";
        });
        Tags("Organizations");
    }
}
