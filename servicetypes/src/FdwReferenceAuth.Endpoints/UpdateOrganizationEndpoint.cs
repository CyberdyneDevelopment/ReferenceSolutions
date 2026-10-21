using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: rename, disable or enable a department (organization) (FDW-1758).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateOrganizationEndpoint : UpdateOrganizationEndpointBase
{
    /// <inheritdoc />
    public UpdateOrganizationEndpoint(
        IOrganizationWriter writer,
        IOrganizationProvider organizations,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles,
        ILogger<UpdateOrganizationEndpoint> logger)
        : base(logger, writer, organizations, audit, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:write");
        Summary(s =>
        {
            s.Summary = "Change a department";
            s.Description = "Renames, disables or enables an organization of the tenant the caller acts in. The default organization cannot be disabled.";
        });
        Tags("Organizations");
    }
}
