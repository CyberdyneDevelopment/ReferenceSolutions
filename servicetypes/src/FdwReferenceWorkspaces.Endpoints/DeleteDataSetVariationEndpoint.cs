using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to delete a variation a workspace owns.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class DeleteDataSetVariationEndpoint : DeleteDataSetVariationEndpointBase
{
    /// <inheritdoc />
    public DeleteDataSetVariationEndpoint(
        ILogger<DeleteDataSetVariationEndpointBase> logger,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IDataSetVariationConfigurationProvider variations)
        : base(logger, workspaces, access, variations)
    {
    }
}
