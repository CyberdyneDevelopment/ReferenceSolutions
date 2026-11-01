using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: assign an organization a document connection for a purpose (FDW-1581).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AssignOrganizationConnectionEndpoint : AssignOrganizationConnectionEndpointBase
{
    /// <inheritdoc />
    public AssignOrganizationConnectionEndpoint(
        IConnectionAssignments assignments,
        IAdministrativeAudit audit,
        ILogger<AssignOrganizationConnectionEndpoint> logger)
        : base(logger, assignments, audit)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:write");
        Summary(s =>
        {
            s.Summary = "Assign an organization a document connection for a purpose";
            s.Description = "Gives an organization one of its tenant's grants for a purpose, with a base folder inside the granted folder.";
        });
        Tags("Organizations");
    }
}
