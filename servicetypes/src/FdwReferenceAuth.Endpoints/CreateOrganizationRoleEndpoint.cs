using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: define a role in an organization (FDW-1593).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateOrganizationRoleEndpoint : CreateOrganizationRoleEndpointBase
{
    /// <inheritdoc />
    public CreateOrganizationRoleEndpoint(
        IOrganizationRoles roles,
        IAdministrativeAudit audit,
        ILogger<CreateOrganizationRoleEndpoint> logger)
        : base(logger, roles, audit)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:write");
        Summary(s =>
        {
            s.Summary = "Define a role in an organization";
            s.Description = "Defines a role with at least one permission, each of which the caller holds. The caller must act in the organization.";
        });
        Tags("Organizations");
    }
}
