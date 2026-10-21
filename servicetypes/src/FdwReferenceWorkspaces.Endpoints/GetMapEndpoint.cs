using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to stream the map of data stores, pipelines and data sets (FDW-1029).</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetMapEndpoint : GetMapEndpointBase
{
    /// <inheritdoc />
    public GetMapEndpoint(
        ILogger<GetMapEndpointBase> logger,
        IDataSetConfigurationProvider dataSets,
        DataStoreConfigurationProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        IDataSetVariationConfigurationProvider variations,
        ICategoryConfigurationProvider categories,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IFrameworkAuthorizationService authorization)
        : base(logger, dataSets, dataStores, pipelines, variations, categories, workspaces, access, authorization)
    {
    }
}
