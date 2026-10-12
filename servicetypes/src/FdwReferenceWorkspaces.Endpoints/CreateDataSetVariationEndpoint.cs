using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to create a data set a workspace owns as a variation of a main data set.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class CreateDataSetVariationEndpoint : CreateDataSetVariationEndpointBase
{
    /// <inheritdoc />
    public CreateDataSetVariationEndpoint(
        ILogger<CreateDataSetVariationEndpointBase> logger,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IDataSetConfigurationProvider dataSets,
        IDataSetVariationConfigurationProvider variations)
        : base(logger, workspaces, access, dataSets, variations)
    {
    }
}
