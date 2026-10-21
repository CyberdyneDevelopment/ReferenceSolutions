using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Fdw.Services.Users.Services;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: make a department (organization) with its starting roles and first administrator (FDW-1758).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateOrganizationEndpoint : CreateOrganizationEndpointBase
{
    /// <inheritdoc />
    public CreateOrganizationEndpoint(
        IOrganizationFounding founding,
        IOrganizationProvider organizations,
        IAdministrativeAudit audit,
        ILogger<CreateOrganizationEndpoint> logger)
        : base(logger, founding, organizations, audit)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:write");
        Summary(s =>
        {
            s.Summary = "Make a department";
            s.Description = "Makes an organization in the tenant the caller acts in, gives it a copy of another organization's roles and names its first administrator, who must already have an account and belong to the tenant.";
        });
        Tags("Organizations");
    }
}
