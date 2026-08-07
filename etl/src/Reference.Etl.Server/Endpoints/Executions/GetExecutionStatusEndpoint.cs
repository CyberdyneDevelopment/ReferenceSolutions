using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Services.Etl.Projects.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Etl.Server.Logging;

namespace Reference.Etl.Server.Endpoints.Executions;

/// <summary>
/// Request for the hierarchical execution status endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetExecutionStatusRequest
{
    public Guid ExecutionId { get; set; }
}

/// <summary>
/// A single node in the recursive execution status tree.
/// Depth 0 = root (Project or pipeline Job). Depth 1 = Stage/Step, etc.
/// The <c>nodeType</c> field lets clients render generically at any depth.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecutionStatusNodeDto
{
    /// <summary>Gets or sets the execution item ID.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the execution item type name (e.g. "Project", "Stage", "Step", "pipeline").
    /// Clients use this for generic rendering without hardcoding depth assumptions.
    /// </summary>
    public string NodeType { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of this execution item.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the current state name.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rollup state derived from child states.
    /// For leaf nodes equals <see cref="State"/>.
    /// </summary>
    public string RollupState { get; set; } = string.Empty;

    /// <summary>Gets or sets the depth of this node (0 = root).</summary>
    public int Depth { get; set; }

    /// <summary>Gets or sets the display ordinal within parent (for ordered types like Stage, Step).</summary>
    public int? Ordinal { get; set; }

    /// <summary>Gets or sets when this execution item started.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Gets or sets when this execution item completed.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Gets or sets the result message (error detail when State=Failed).</summary>
    public string? ResultMessage { get; set; }

    /// <summary>
    /// Gets or sets the child nodes.
    /// Recursive shape: Stages under Project, Steps under Stage, Pipelines under Step, etc.
    /// Any new execution item type depth is automatically included.
    /// </summary>
    public IList<ExecutionStatusNodeDto> Children { get; set; } = new List<ExecutionStatusNodeDto>();
}

/// <summary>
/// <c>GET /etl/executions/{executionId}</c> — hierarchical rollup execution status.
///
/// <para>
/// Returns the full recursive status tree for any execution item:
/// project executions return the Project → Stage → Step → Pipeline tree;
/// pipeline-only executions return a flat single-node response.
/// The recursive <c>children</c> shape supports any future depth additions.
/// </para>
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetExecutionStatusEndpoint : Endpoint<GetExecutionStatusRequest, ExecutionStatusNodeDto>
{
    private readonly IExecutionTracker _executionTracker;
    private readonly IProjectExecutionStatusReader _projectStatusReader;
    private readonly ILogger<GetExecutionStatusEndpoint> _logger;

    public GetExecutionStatusEndpoint(
        IExecutionTracker executionTracker,
        IProjectExecutionStatusReader projectStatusReader,
        ILogger<GetExecutionStatusEndpoint> logger)
    {
        _executionTracker = executionTracker;
        _projectStatusReader = projectStatusReader;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<GetExecutionStatusEndpoint>.Instance;
    }

    public override void Configure()
    {
        Get("etl/executions/{ExecutionId}");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "Get execution status";
            s.Description = "Returns the recursive status tree for any execution item. " +
                             "Project executions include Stage → Step → Pipeline children. " +
                             "Pipeline-only executions return a flat single-node response. " +
                             "The nodeType field allows generic rendering at any depth.";
        });
    }

    public override async Task HandleAsync(GetExecutionStatusRequest req, CancellationToken ct)
    {
        EtlServerLog.ExecutionStatusRequestReceived(_logger, req.ExecutionId);

        var itemResult = await _executionTracker.GetItem(req.ExecutionId, ct).ConfigureAwait(false);
        if (!itemResult.IsSuccess || itemResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var item = itemResult.Value;

        // Why: check if this is a project-type execution; if so, build the full hierarchical tree.
        // All other types (Job, Workflow, Task) fall through to flat single-node response.
        var isProjectExecution = string.Equals(item.ItemType?.Name, "Project", StringComparison.OrdinalIgnoreCase);

        if (isProjectExecution)
        {
            var treeResult = await _projectStatusReader.GetStatusTree(req.ExecutionId, ct).ConfigureAwait(false);
            if (!treeResult.IsSuccess || treeResult.Value is null)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            await Send.OkAsync(MapNode(treeResult.Value), ct).ConfigureAwait(false);
            return;
        }

        // Why: flat single-node for pipeline/job/workflow executions — no children to walk.
        var dto = new ExecutionStatusNodeDto
        {
            ExecutionId = item.Id,
            NodeType = item.ItemType?.Name ?? "Unknown",
            Name = item.Name,
            State = item.State.Name,
            RollupState = item.State.Name,
            Depth = 0,
            StartedAt = item.StartedAt?.DateTime,
            CompletedAt = item.CompletedAt?.DateTime,
            ResultMessage = item.ResultMessage
        };

        await Send.OkAsync(dto, ct).ConfigureAwait(false);
    }

    private static ExecutionStatusNodeDto MapNode(ProjectExecutionStatusNode node)
    {
        var dto = new ExecutionStatusNodeDto
        {
            ExecutionId = node.ExecutionItem.Id,
            NodeType = node.ExecutionItem.ItemType?.Name ?? "Unknown",
            Name = node.ExecutionItem.Name,
            State = node.ExecutionItem.State.Name,
            RollupState = node.RollupState,
            Depth = node.Depth,
            Ordinal = node.Ordinal,
            StartedAt = node.ExecutionItem.StartedAt?.DateTime,
            CompletedAt = node.ExecutionItem.CompletedAt?.DateTime,
            ResultMessage = node.ExecutionItem.ResultMessage
        };

        foreach (var child in node.Children)
        {
            dto.Children.Add(MapNode(child));
        }

        return dto;
    }
}
