using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Writers;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceEtl.Endpoints.Nodes.Logging;

namespace FdwReferenceEtl.Endpoints.Nodes;

/// <summary>
/// Request to create a new orchestration node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateOrchestrationNodeRequest
{
    /// <summary>Gets or sets the node name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the node type ID (references OrchestrationNodeTypes).</summary>
    public int? NodeTypeId { get; set; }

    /// <summary>Gets or sets what the node is, by name: Project, Stage or Step (or a node type a developer added). Send this or <see cref="NodeTypeId"/>; if both are sent they must name the same type.</summary>
    public string? NodeType { get; set; }

    /// <summary>Gets or sets the optional parent's durable logical Id. The physical RowId is DB-managed
    /// and invisible to the application, so the parent is addressed by its durable Id only.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the display ordinal among siblings.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the pipelines the node holds, by pipeline name. Only a step can hold pipelines; leave it out for a node that holds none.</summary>
    public List<NodePipelineRequest>? Pipelines { get; set; }

    /// <summary>Gets or sets the prerequisites between the node's pipelines, by pipeline name: each pipeline starts only after the pipeline it needs has succeeded.</summary>
    public List<NodePrerequisiteRequest>? Prerequisites { get; set; }
}

/// <summary>
/// Creates a new orchestration node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateOrchestrationNodeEndpoint : Endpoint<CreateOrchestrationNodeRequest, OrchestrationNodeImplementationConfiguration>
{
    private readonly OrchestrationNodeConfigurationWriter _writer;
    private readonly IPipelineConfigurationProvider _pipelines;
    private readonly ILogger<CreateOrchestrationNodeEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateOrchestrationNodeEndpoint"/> class.
    /// </summary>
    public CreateOrchestrationNodeEndpoint(
        OrchestrationNodeConfigurationWriter writer,
        IPipelineConfigurationProvider pipelines,
        ILogger<CreateOrchestrationNodeEndpoint> logger)
    {
        _writer = writer;
        _pipelines = pipelines;
        _logger = logger ?? NullLogger<CreateOrchestrationNodeEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/nodes");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("projects:write");
#endif
        Summary(s =>
        {
            s.Summary = "Create an orchestration node";
            s.Description = "Creates a new orchestration node of any type.";
        });
        Tags("Nodes");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CreateOrchestrationNodeRequest req, CancellationToken ct)
    {
        NodeLog.CreatingNode(_logger, req.Name);

        var nodeType = NodeTypes.Resolve(req.NodeType, req.NodeTypeId, req.Name, _logger);
        if (nodeType.IsFailure)
        {
            NodeLog.NodeCreateFailed(_logger, req.Name, nodeType.CurrentMessage);
            var (typeStatus, typeProblem) = ResultHttpStatusMapper.Map(nodeType, HttpContext);
            await Send.ResultAsync(TypedResults.Json(typeProblem, statusCode: typeStatus)).ConfigureAwait(false);
            return;
        }

        var config = new OrchestrationNodeImplementationConfiguration
        {
            // Why: API contract — IDs are uuid v7 for time-orderable persistence.
            Id = Guid.CreateVersion7(),
            // The implementation the domain provider is registered under -- OrchestrationNode has one
            // (DefaultOrchestrationServiceType.Register("OrchestrationNode", ...)), not one per node type.
            // Save looks the name up before writing; "" is registered for nothing. (FDW-790)
            Implementation = "OrchestrationNode",
            NodeTypeId = nodeType.Value,
            Name = req.Name,
            Description = req.Description,
            Ordinal = req.Ordinal,
            // The parent node is cited by its durable Id; the table stores it as it is.
            ParentId = req.ParentId,
            IsEnabled = true,
        };

        var bound = await NodePipelines.Apply(_pipelines, config, req.Pipelines, req.Prerequisites, _logger, ct).ConfigureAwait(false);
        if (bound.IsFailure)
        {
            NodeLog.NodeCreateFailed(_logger, req.Name, bound.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(bound, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        // Why the writer: it validates the node (name, parent, policy elevation, prerequisites) before anything is stored; the provider alone does not.
        var result = await _writer.Save(config, ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            NodeLog.NodeCreateFailed(_logger, req.Name, result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        NodeLog.NodeCreated(_logger, req.Name, config.Id);
        await Send.CreatedAtAsync<GetOrchestrationNodeEndpoint>(
            new { id = config.Id },
            config,
            cancellation: ct).ConfigureAwait(false);
    }
}
