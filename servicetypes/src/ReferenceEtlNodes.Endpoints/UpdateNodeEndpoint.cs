using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceEtlJobs.Endpoints.Logging;

namespace ReferenceEtlNodes.Endpoints;

/// <summary>
/// Request for updating an orchestration node. NodeType is immutable after creation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateNodeRequest
{
    public Guid Id { get; set; }
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
/// <c>PUT /etl/nodes/{id}</c> — update an existing orchestration node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateNodeEndpoint : Endpoint<UpdateNodeRequest, NodeDto>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly ILogger<UpdateNodeEndpoint> _logger;

    public UpdateNodeEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        ILogger<UpdateNodeEndpoint> logger)
    {
        _provider = provider;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<UpdateNodeEndpoint>.Instance;
    }

    public override void Configure()
    {
        Put("etl/nodes/{id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:write");
#endif
        Summary(s =>
        {
            s.Summary = "Update an orchestration node";
            s.Description = "Updates an existing orchestration node. " +
                             "NodeType is immutable — use DELETE + POST to change type. " +
                             "ParentId is also immutable via PUT; re-parent via DELETE + POST.";
        });
    }

    public override async Task HandleAsync(UpdateNodeRequest req, CancellationToken ct)
    {
        EtlServerLog.UpdateNodeRequestReceived(_logger, req.Id);

        // Why: load the existing node first to preserve immutable fields (NodeTypeId, ParentRowId, ParentId).
        // Clients are not allowed to reparent or retype a node via PUT.
        var existingResult = await _provider.Get(req.Id, ct).ConfigureAwait(false);
        if (!existingResult.IsSuccess || existingResult.Value is null)
        {
            EtlServerLog.NodeNotFound(_logger, req.Id);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var existing = existingResult.Value;

        var config = new OrchestrationNodeConfiguration
        {
            Id = req.Id,
            // Why: NodeTypeId and parent linkage are immutable on PUT — preserve from existing record.
            // ParentId is the logical FK; the physical ParentRowId is data-only (not a POCO property).
            NodeTypeId = existing.NodeTypeId,
            ParentId = existing.ParentId,
            Name = req.Name,
            Description = req.Description,
            Ordinal = req.Ordinal,
            IsEnabled = req.IsEnabled,
            TenantId = req.TenantId,
            StepFailurePolicy = req.StepFailurePolicy,
            StageFailurePolicy = req.StageFailurePolicy,
            MaxParallelPipelines = req.MaxParallelPipelines,
            RequireApprovalToRun = req.RequireApprovalToRun,
            AllowResume = req.AllowResume,
            AllowCrossTenant = req.AllowCrossTenant,
            ResiliencyPolicyId = req.ResiliencyPolicyId
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

        EtlServerLog.NodeUpdated(_logger, req.Id);
        await Send.OkAsync(CreateNodeEndpoint.MapToDto(result.Value), ct).ConfigureAwait(false);
    }
}
