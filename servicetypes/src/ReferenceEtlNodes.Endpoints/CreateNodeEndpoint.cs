using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceEtlJobs.Endpoints.Logging;

namespace ReferenceEtlNodes.Endpoints;

/// <summary>
/// Request for creating a generic orchestration node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateNodeRequest
{
    public string NodeType { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Ordinal { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? StepFailurePolicy { get; set; }
    public string? StageFailurePolicy { get; set; }
    public int? MaxParallelPipelines { get; set; }
    public bool? RequireApprovalToRun { get; set; }
    public bool? AllowResume { get; set; }
    public bool? AllowCrossTenant { get; set; }
    public Guid? ResiliencyPolicyId { get; set; }
    public Guid? TenantId { get; set; }
}

/// <summary>
/// Generic orchestration node response DTO.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class NodeDto
{
    public Guid Id { get; set; }
    public int NodeTypeId { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Ordinal { get; set; }
    public bool IsEnabled { get; set; }
    public string? StepFailurePolicy { get; set; }
    public string? StageFailurePolicy { get; set; }
    public int? MaxParallelPipelines { get; set; }
    public bool? RequireApprovalToRun { get; set; }
    public bool? AllowResume { get; set; }
    public bool? AllowCrossTenant { get; set; }
    public Guid? ResiliencyPolicyId { get; set; }
    public Guid? TenantId { get; set; }
}

/// <summary>
/// <c>POST /etl/nodes</c> — create a new generic orchestration node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateNodeEndpoint : Endpoint<CreateNodeRequest, NodeDto>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly ILogger<CreateNodeEndpoint> _logger;

    public CreateNodeEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        ILogger<CreateNodeEndpoint> logger)
    {
        _provider = provider;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<CreateNodeEndpoint>.Instance;
    }

    public override void Configure()
    {
        Post("etl/nodes");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:write");
#endif
        Summary(s =>
        {
            s.Summary = "Create an orchestration node";
            s.Description = "Creates a new orchestration node of any registered type. " +
                             "NodeType is resolved via OrchestrationNodeTypes.ByName() — " +
                             "new types are supported without code changes.";
        });
    }

    public override async Task HandleAsync(CreateNodeRequest req, CancellationToken ct)
    {
        EtlServerLog.CreateNodeRequestReceived(_logger, req.Name, req.NodeType);

        // Why: resolve the NodeTypeId from the TypeCollection before building the config.
        // This keeps the connection-type-invisible principle — no switch on type strings in logic.
        var nodeType = OrchestrationNodeTypes.ByName(req.NodeType);
        if (nodeType == OrchestrationNodeTypes.NotFound)
        {
            EtlServerLog.NodeTypeNotFound(_logger, req.NodeType);
            AddError($"NodeType '{req.NodeType}' is not a registered OrchestrationNodeType.");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        var config = new OrchestrationNodeConfiguration
        {
            NodeTypeId = nodeType.Id,
            ParentId = req.ParentId,
            Name = req.Name,
            Description = req.Description,
            Ordinal = req.Ordinal,
            IsEnabled = req.IsEnabled,
            StepFailurePolicy = req.StepFailurePolicy,
            StageFailurePolicy = req.StageFailurePolicy,
            MaxParallelPipelines = req.MaxParallelPipelines,
            RequireApprovalToRun = req.RequireApprovalToRun,
            AllowResume = req.AllowResume,
            AllowCrossTenant = req.AllowCrossTenant,
            ResiliencyPolicyId = req.ResiliencyPolicyId,
            TenantId = req.TenantId
        };

        var result = await _provider.Save(config, ct).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            EtlServerLog.NodeSaveFailed(_logger,
                new InvalidOperationException(EtlServerLog.GetError(result, _logger)),
                EtlServerLog.GetError(result, _logger));
            AddError(EtlServerLog.GetError(result, _logger));
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        var saved = result.Value;
        EtlServerLog.NodeCreated(_logger, saved.Name, saved.Id);
        await Send.CreatedAtAsync<GetNodeEndpoint>(
            new { id = saved.Id },
            MapToDto(saved, req.NodeType),
            cancellation: ct).ConfigureAwait(false);
    }

    internal static NodeDto MapToDto(OrchestrationNodeConfiguration config, string? nodeTypeName = null)
    {
        // Why: resolve display name from TypeCollection for the response; fall back to
        // empty string if the type is no longer registered (defensive, should not occur at runtime).
        var resolvedType = OrchestrationNodeTypes.ById(config.NodeTypeId);
        var resolvedTypeName = nodeTypeName
            ?? (resolvedType == OrchestrationNodeTypes.NotFound ? string.Empty : resolvedType.Name);

        return new NodeDto
        {
            Id = config.Id,
            NodeTypeId = config.NodeTypeId,
            NodeType = resolvedTypeName,
            ParentId = config.ParentId,
            Name = config.Name,
            Description = config.Description,
            Ordinal = config.Ordinal,
            IsEnabled = config.IsEnabled,
            StepFailurePolicy = config.StepFailurePolicy,
            StageFailurePolicy = config.StageFailurePolicy,
            MaxParallelPipelines = config.MaxParallelPipelines,
            RequireApprovalToRun = config.RequireApprovalToRun,
            AllowResume = config.AllowResume,
            AllowCrossTenant = config.AllowCrossTenant,
            ResiliencyPolicyId = config.ResiliencyPolicyId,
            TenantId = config.TenantId
        };
    }
}
