using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to count what the map holds, per kind, without reading any of it (FDW-1195).</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetMapSummaryEndpoint : GetMapSummaryEndpointBase
{
    /// <inheritdoc />
    public GetMapSummaryEndpoint(
        ILogger<GetMapSummaryEndpointBase> logger,
        IDataSetConfigurationProvider dataSets,
        DataStoreConfigurationProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        IDataSetVariationConfigurationProvider variations,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IConnectionConfigurationProvider connections,
        IQualityRuleConfigurationProvider qualityRules,
        IScheduleConfigurationProvider schedules)
        : base(logger, dataSets, dataStores, pipelines, variations, workspaces, access, connections, qualityRules, schedules)
    {
    }
}
