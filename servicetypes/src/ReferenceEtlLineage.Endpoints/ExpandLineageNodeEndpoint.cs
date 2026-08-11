using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.Lineage;
using Fdw.Operations.Endpoints;
using Fdw.Services.Etl.Projects.Lineage;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceEtlJobs.Endpoints.Logging;

namespace ReferenceEtlLineage.Endpoints;

/// <summary>
/// Request for expanding a single lineage node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExpandLineageNodeRequest
{
    /// <summary>The type of the node to expand (e.g., Project, Stage, Step, Pipeline, DataSet).</summary>
    public string NodeType { get; set; } = string.Empty;

    /// <summary>The name-based identifier of the node to expand.</summary>
    public string NodeId { get; set; } = string.Empty;
}

/// <summary>
/// <c>GET /etl/lineage/expand</c> — returns the direct neighbors (upstream and downstream)
/// of a single lineage node for lazy UI tree expansion.
///
/// Query params: <c>nodeType</c> and <c>nodeId</c>.
/// </summary>
/// <remarks>
/// Why a separate endpoint instead of reusing LineageGraphEndpoint: lazy expansion only fetches
/// direct neighbors for a clicked node, avoiding the full transitive subgraph traversal on every
/// UI interaction. The full graph is still built server-side (with caching), but only the
/// adjacent edges are returned.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ExpandLineageNodeEndpoint : Endpoint<ExpandLineageNodeRequest, LineageGraphResponse>
{
    // Why: inject a LineageGraphEndpoint instance to reuse the BuildFullGraph logic.
    // ProjectLineageGraphEndpointBase.BuildFullGraph is protected — subclassing is the
    // only way to call it without duplicating the 7-table parallel query. We expose it
    // via the inner builder class below.
    private readonly GraphBuilder _builder;
    private readonly ILogger<ExpandLineageNodeEndpoint> _logger;

    public ExpandLineageNodeEndpoint(
        IConfigurationGateway configurationGateway,
        Fdw.Services.Pipelines.PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<ExpandLineageNodeEndpoint> logger)
    {
        _builder = new GraphBuilder(configurationGateway, pipelineProvider, NullLogger<ProjectLineageGraphEndpointBase>.Instance);
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<ExpandLineageNodeEndpoint>.Instance;
    }

    public override void Configure()
    {
        Get("etl/lineage/expand");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Expand a lineage node";
            s.Description = "Returns the direct upstream and downstream neighbors of a single lineage node. " +
                             "Use nodeType and nodeId query parameters. " +
                             "Intended for lazy tree expansion in lineage UIs.";
        });
    }

    public override async Task HandleAsync(ExpandLineageNodeRequest req, CancellationToken ct)
    {
        EtlServerLog.ExpandLineageNodeRequestReceived(_logger, req.NodeType, req.NodeId);

        // Build the combined node ID used in the graph. Format: "{NodeType}_{NodeId}"
        // This mirrors how ProjectLineageGraphEndpointBase and GetLineageGraphEndpointBase
        // construct node IDs (e.g. "Pipeline_MyPipeline", "Project_MyProject").
        var entryNodeId = $"{req.NodeType}_{req.NodeId}";

        var graph = await _builder.BuildGraph(ct).ConfigureAwait(false);

        var entryNode = graph.FindNode(entryNodeId);
        if (entryNode == null)
        {
            EtlServerLog.LineageNodeNotFound(_logger, req.NodeType, req.NodeId);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        // Return only direct neighbors (single-hop), not the full transitive subgraph.
        // Why: the expand endpoint is for lazy loading; the full lineage endpoint returns
        // transitive graphs. Returning just direct neighbors keeps the payload small.
        var directUpstream = graph.GetUpstream(entryNodeId).ToList();
        var directDownstream = graph.GetDownstream(entryNodeId).ToList();

        var neighborNodes = directUpstream.Concat(directDownstream)
            .Append(entryNode)
            .DistinctBy(n => n.Id, StringComparer.Ordinal)
            .ToList();

        var neighborNodeIds = neighborNodes
            .Select(n => n.Id)
            .ToHashSet(StringComparer.Ordinal);

        var neighborEdges = graph.Edges
            .Where(e => neighborNodeIds.Contains(e.SourceId) && neighborNodeIds.Contains(e.TargetId))
            .ToList();

        // Why: inline the mapping rather than calling GetLineageGraphEndpointBase.MapToResponse —
        // that method is internal to the compiled assembly and not accessible from here.
        var response = new LineageGraphResponse
        {
            Nodes = neighborNodes.Select(n => new LineageGraphNodeResponse
            {
                Id = n.Id,
                Label = n.Name,
                Type = n.Type.Name,
                Category = n.Description,
                Properties = n.Metadata != null
                    ? n.Metadata
                        .Where(kv => kv.Value != null)
                        .ToDictionary(kv => kv.Key, kv => kv.Value!, StringComparer.Ordinal)
                    : new Dictionary<string, object>(StringComparer.Ordinal)
            }).ToList(),
            Edges = neighborEdges.Select(e => new LineageGraphEdgeResponse
            {
                SourceId = e.SourceId,
                TargetId = e.TargetId,
                Relation = e.Type.Name,
                Properties = new Dictionary<string, object>(StringComparer.Ordinal)
            }).ToList()
        };
        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Inner class that provides public access to <see cref="ProjectLineageGraphEndpointBase.BuildFullGraph"/>.
    /// Why inner class: <c>BuildFullGraph</c> is protected on the abstract base. Subclassing here
    /// gives this endpoint access without exposing the method publicly or duplicating the parallel
    /// query logic.
    /// </summary>
    [FastEndpoints.DontRegister]
    private sealed class GraphBuilder : ProjectLineageGraphEndpointBase
    {
        public GraphBuilder(
            IConfigurationGateway configurationGateway,
            Fdw.Services.Pipelines.PipelineServiceConfigurationProvider pipelineProvider,
            ILogger<ProjectLineageGraphEndpointBase> logger)
            : base(configurationGateway, pipelineProvider, logger)
        {
        }

        public override void Configure()
        {
            // Why: [DontRegister] keeps FastEndpoints from discovering this nested helper as an
            // actual endpoint; Configure can stay empty.
        }

        public Task<LineageGraph> BuildGraph(CancellationToken ct) =>
            BuildFullGraph(ct);
    }
}
