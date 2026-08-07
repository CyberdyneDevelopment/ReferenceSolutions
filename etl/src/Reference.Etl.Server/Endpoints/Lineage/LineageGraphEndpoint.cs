using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Projects.Lineage;
using Microsoft.Extensions.Logging;

namespace Reference.Etl.Server.Endpoints.Lineage;

/// <summary>
/// <c>GET /etl/lineage/{EntityType}/{EntityName}</c> — returns the transitive lineage graph
/// for a specific entity, including Project/Stage/Step/Pipeline orchestration nodes.
///
/// Inherits from <see cref="ProjectLineageGraphEndpointBase"/> which extends the base
/// DataSet/Pipeline/Connection/Calculation graph with pipe.* orchestration data.
/// </summary>
/// <remarks>
/// Why inherit from ProjectLineageGraphEndpointBase and not GetLineageGraphEndpointBase:
/// The ETL server owns project/stage/step orchestration. The project-aware base adds those
/// nodes and edges from the pipe.* tables without circular dependency on this assembly.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class LineageGraphEndpoint : ProjectLineageGraphEndpointBase
{
    /// <inheritdoc />
    public LineageGraphEndpoint(
        IConfigurationGateway configurationGateway,
        Fdw.Services.Pipelines.PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<LineageGraphEndpoint> logger)
        : base(configurationGateway, pipelineProvider, logger)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        // Why: override Configure to apply the ETL-specific route prefix (etl/) and
        // use the pipelines:read policy consistent with other ETL read endpoints.
        Get("etl/lineage/{EntityType}/{EntityName}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get entity lineage graph";
            s.Description = "Returns the transitive lineage graph for a specific entity, " +
                             "including Project/Stage/Step orchestration nodes. " +
                             "EntityType values: DataSet, Pipeline, Connection, Calculation, Project, Stage, Step. " +
                             "EntityName is the logical name of the entity.";
        });
    }
}
