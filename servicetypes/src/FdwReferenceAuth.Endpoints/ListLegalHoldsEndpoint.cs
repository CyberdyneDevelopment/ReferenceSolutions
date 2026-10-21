using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: list the legal holds of an organization (FDW-1575).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListLegalHoldsEndpoint : ListLegalHoldsEndpointBase
{
    /// <inheritdoc />
    public ListLegalHoldsEndpoint(
        ILegalHolds holds,
        IOrganizationProvider organizations,
        ISystemRoleConfiguration systemRoles,
        ILogger<ListLegalHoldsEndpoint> logger)
        : base(logger, holds, organizations, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("legal-holds:read");
        Summary(s =>
        {
            s.Summary = "List the legal holds of an organization";
            s.Description = "Lists the legal holds on an organization's documents, newest first; heldOnly=true leaves out the holds already lifted.";
        });
        Tags("Retention");
    }
}
