using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: list the document connections assigned to an organization (FDW-1581).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListOrganizationConnectionsEndpoint : ListOrganizationConnectionsEndpointBase
{
    /// <inheritdoc />
    public ListOrganizationConnectionsEndpoint(
        IConnectionAssignments assignments,
        ILogger<ListOrganizationConnectionsEndpoint> logger)
        : base(logger, assignments)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:read");
        Summary(s =>
        {
            s.Summary = "List the document connections assigned to an organization";
            s.Description = "Lists the connections assigned to an organization, one per purpose.";
        });
        Tags("Organizations");
    }
}
