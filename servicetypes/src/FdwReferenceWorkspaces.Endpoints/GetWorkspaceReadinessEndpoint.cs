using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to read a workspace's readiness.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetWorkspaceReadinessEndpoint : GetWorkspaceReadinessEndpointBase
{
    /// <inheritdoc />
    public GetWorkspaceReadinessEndpoint(
        ILogger<GetWorkspaceReadinessEndpointBase> logger,
        WorkspaceConfigurationProvider workspaces,
        IDataSetConfigurationProvider dataSets,
        IWorkspaceAccessPolicy access)
        : base(logger, workspaces, dataSets, access)
    {
    }
}
