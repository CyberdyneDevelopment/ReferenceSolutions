using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: take a purpose's document connection away from an organization (FDW-1581).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UnassignOrganizationConnectionEndpoint : UnassignOrganizationConnectionEndpointBase
{
    /// <inheritdoc />
    public UnassignOrganizationConnectionEndpoint(
        IConnectionAssignments assignments,
        IAdministrativeAudit audit,
        ILogger<UnassignOrganizationConnectionEndpoint> logger)
        : base(logger, assignments, audit)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:write");
        Summary(s =>
        {
            s.Summary = "Take a purpose's document connection away from an organization";
            s.Description = "Removes the connection an organization was assigned for a purpose.";
        });
        Tags("Organizations");
    }
}
