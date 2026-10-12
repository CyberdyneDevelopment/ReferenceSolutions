using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to explode one node of the map (FDW-1029).</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetMapChildrenEndpoint : GetMapChildrenEndpointBase
{
    /// <inheritdoc />
    public GetMapChildrenEndpoint(
        ILogger<GetMapChildrenEndpointBase> logger,
        IDataSetConfigurationProvider dataSets,
        DataStoreConfigurationProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        IDataSetVariationConfigurationProvider variations,
        IFrameworkAuthorizationService authorization)
        : base(logger, dataSets, dataStores, pipelines, variations, authorization)
    {
    }
}
