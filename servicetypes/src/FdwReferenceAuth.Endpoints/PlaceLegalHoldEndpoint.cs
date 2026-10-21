using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: put a document under a legal hold (FDW-1575).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PlaceLegalHoldEndpoint : PlaceLegalHoldEndpointBase
{
    /// <inheritdoc />
    public PlaceLegalHoldEndpoint(
        ILegalHolds holds,
        IOrganizationProvider organizations,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles,
        ILogger<PlaceLegalHoldEndpoint> logger)
        : base(logger, holds, organizations, audit, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("legal-holds:write");
        Summary(s =>
        {
            s.Summary = "Put a document under a legal hold";
            s.Description = "Puts a document under a legal hold, so the retention job does not end it however long past its period. The hold is audited with its reason.";
        });
        Tags("Retention");
    }
}
