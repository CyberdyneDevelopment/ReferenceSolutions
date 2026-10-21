using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Projects.Abstractions;
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
/// Request to update an existing orchestration node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateOrchestrationNodeRequest
{
    /// <summary>Gets or sets the node identifier (from route).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the new name, or null to leave unchanged.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the new description, or null to leave unchanged.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the new ordinal, or null to leave unchanged.</summary>
    public int? Ordinal { get; set; }

    /// <summary>Gets or sets the enabled state, or null to leave unchanged.</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>Gets or sets the pipelines the node holds, by pipeline name; sending it replaces the node's pipelines, leaving it out keeps them. Only a step can hold pipelines.</summary>
    public List<NodePipelineRequest>? Pipelines { get; set; }

    /// <summary>Gets or sets the prerequisites between the node's pipelines, by pipeline name; sending it replaces the node's prerequisites, leaving it out keeps them.</summary>
    public List<NodePrerequisiteRequest>? Prerequisites { get; set; }
}

/// <summary>
/// Updates an existing orchestration node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateOrchestrationNodeEndpoint : Endpoint<UpdateOrchestrationNodeRequest, OrchestrationNodeImplementationConfiguration>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly OrchestrationNodeConfigurationWriter _writer;
    private readonly IPipelineConfigurationProvider _pipelines;
    private readonly ILogger<UpdateOrchestrationNodeEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateOrchestrationNodeEndpoint"/> class.
    /// </summary>
    public UpdateOrchestrationNodeEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        OrchestrationNodeConfigurationWriter writer,
        IPipelineConfigurationProvider pipelines,
        ILogger<UpdateOrchestrationNodeEndpoint> logger)
    {
        _provider = provider;
        _writer = writer;
        _pipelines = pipelines;
        _logger = logger ?? NullLogger<UpdateOrchestrationNodeEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/nodes/{id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("projects:write");
#endif
        Summary(s =>
        {
            s.Summary = "Update an orchestration node";
            s.Description = "Updates fields on an existing orchestration node.";
        });
        Tags("Nodes");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(UpdateOrchestrationNodeRequest req, CancellationToken ct)
    {
        NodeLog.UpdatingNode(_logger, req.Id);

        var getResult = await _provider.Get(req.Id, ct).ConfigureAwait(false);
        if (getResult.IsFailure)
        {
            NodeLog.NodeGetFailed(_logger, req.Id, getResult.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        if (getResult.Value is null)
        {
            NodeLog.NodeNotFoundForUpdate(_logger, req.Id);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var existing = getResult.Value;

        if (req.Name != null)
            existing.Name = req.Name;
        if (req.Description != null)
            existing.Description = req.Description;
        if (req.Ordinal.HasValue)
            existing.Ordinal = req.Ordinal.Value;
        if (req.IsEnabled.HasValue)
            existing.IsEnabled = req.IsEnabled.Value;

        if (existing is not OrchestrationNodeImplementationConfiguration node)
        {
            NodeLog.NodeUpdateFailed(_logger, req.Id, $"resolved to '{existing.GetType().Name}'");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        var bound = await NodePipelines.Apply(_pipelines, node, req.Pipelines, req.Prerequisites, _logger, ct).ConfigureAwait(false);
        if (bound.IsFailure)
        {
            NodeLog.NodeUpdateFailed(_logger, req.Id, bound.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(bound, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        // Why the writer: it validates the node (name, parent, policy elevation, prerequisites) before anything is stored; the provider alone does not.
        var saveResult = await _writer.Save(node, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            NodeLog.NodeUpdateFailed(_logger, req.Id, saveResult.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(saveResult, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        NodeLog.NodeUpdated(_logger, req.Id);
        await Send.OkAsync(node, ct).ConfigureAwait(false);
    }
}
