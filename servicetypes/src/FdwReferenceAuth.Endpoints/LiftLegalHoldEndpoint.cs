using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: lift the legal hold on a document (FDW-1575).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class LiftLegalHoldEndpoint : LiftLegalHoldEndpointBase
{
    /// <inheritdoc />
    public LiftLegalHoldEndpoint(
        ILegalHolds holds,
        IOrganizationProvider organizations,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles,
        ILogger<LiftLegalHoldEndpoint> logger)
        : base(logger, holds, organizations, audit, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("legal-holds:write");
        Summary(s =>
        {
            s.Summary = "Lift the legal hold on a document";
            s.Description = "Lifts the legal hold on a document. The hold stays on record with who lifted it and when; the release is audited.";
        });
        Tags("Retention");
    }
}
