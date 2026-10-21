using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: run the retention job for an organization (FDW-1575).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RunRetentionEndpoint : RunRetentionEndpointBase
{
    /// <inheritdoc />
    public RunRetentionEndpoint(
        IRetentionKeeper keeper,
        IOrganizationProvider organizations,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles,
        ILogger<RunRetentionEndpoint> logger)
        : base(logger, keeper, organizations, audit, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("retention:write");
        Summary(s =>
        {
            s.Summary = "Run the retention job for an organization";
            s.Description = "Ends the organization's documents that are past their retention period and not under a legal hold, by asking the application's documents to end them. Each document ended is audited.";
        });
        Tags("Retention");
    }
}
