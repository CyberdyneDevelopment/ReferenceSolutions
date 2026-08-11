using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data.Extensions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Notifications;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceEtlJobs.Endpoints;
using ReferenceEtlJobs.Endpoints.Logging;

namespace ReferenceEtlJobs.Endpoints;

/// <summary>
/// Deprecated trigger request body — kept for one release cycle of backwards compatibility.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TriggerJobRequest
{
    public string PipelineName { get; set; } = string.Empty;
    public string? TriggerSource { get; set; }
    public string? ScheduleName { get; set; }
}

/// <summary>
/// Deprecated trigger response — kept for one release cycle of backwards compatibility.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TriggerJobResponse
{
    public Guid ExecutionId { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Deprecated: <c>POST /etl/trigger</c> — pipeline-only trigger shim.
/// Will be removed in the next release cycle; use <c>POST /etl/trigger/{type}</c>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TriggerJobEndpoint : Endpoint<TriggerJobRequest, TriggerJobResponse>
{
    private const string EtlContainerName = "PipelineExecution";

    private readonly IExecutionTracker _executionTracker;
    private readonly IDataGateway _dataGateway;
    private readonly IFdwServiceProvider<IEtlPipeline, PipelineConfiguration> _pipelineProvider;
    private readonly IPipelineExecutionQueue _pipelineQueue;
    private readonly IPipelineStatusBroadcaster _broadcaster;
    private readonly ILogger<TriggerJobEndpoint> _logger;

    public TriggerJobEndpoint(
        IExecutionTracker executionTracker,
        IDataGateway dataGateway,
        IFdwServiceProvider<IEtlPipeline, PipelineConfiguration> pipelineProvider,
        IPipelineExecutionQueue pipelineQueue,
        IPipelineStatusBroadcaster broadcaster,
        ILogger<TriggerJobEndpoint> logger)
    {
        _executionTracker = executionTracker;
        _dataGateway = dataGateway;
        _pipelineProvider = pipelineProvider;
        _pipelineQueue = pipelineQueue;
        _broadcaster = broadcaster;
        _logger = logger ?? NullLogger<TriggerJobEndpoint>.Instance;
    }

    public override void Configure()
    {
        Post("etl/trigger");
        // Why: requires authentication like every other ETL data endpoint (no anonymous access);
        // pipelines:execute matches the unified trigger it aliases.
        Policies("pipelines:execute");
        Summary(s =>
        {
            s.Summary = "[DEPRECATED] Trigger a pipeline ETL job";
            s.Description = "Deprecated alias for POST /etl/trigger/pipeline. Use POST /etl/trigger/{type} instead.";
            s.ExampleRequest = new TriggerJobRequest
            {
                PipelineName = "NflDataPipeline",
                TriggerSource = "Api"
            };
        });
    }

    public override async Task HandleAsync(TriggerJobRequest req, CancellationToken ct)
    {
        EtlServerLog.TriggerJobRequestReceived(_logger, req.PipelineName);

        var unifiedReq = new UnifiedTriggerRequest
        {
            Name = req.PipelineName,
            TriggerSource = req.TriggerSource
        };

        var result = await TriggerPipeline(unifiedReq, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            AddError(EtlServerLog.GetError(result, _logger));
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        var triggerResult = result.Value!;
        var httpStatus = string.Equals(triggerResult.Status, "QueueFull", StringComparison.Ordinal) ? 503 : 202;

        await Send.ResponseAsync(new TriggerJobResponse
        {
            ExecutionId = triggerResult.ExecutionId,
            Status = triggerResult.Status
        }, httpStatus, ct).ConfigureAwait(false);
    }

    // Why: FastEndpoints 8 does not register endpoint classes as DI services, so
    // UnifiedTriggerEndpoint cannot be injected here. Duplicate the pipeline-trigger
    // logic directly on this deprecated shim to avoid the DI resolution failure.
    // Remove this entire file when the deprecated /etl/trigger route is retired.
    private async Task<IGenericResult<UnifiedTriggerResponse>> TriggerPipeline(
        UnifiedTriggerRequest req,
        CancellationToken ct)
    {
        var pipelineName = req.Name ?? string.Empty;
        var triggerSource = req.TriggerSource ?? "Api";

        // Why: mirrors UnifiedTriggerEndpoint.TriggerPipeline — the caller's own authenticated tenant
        // claim is the only trusted source here (this deprecated shim's TriggerJobRequest carries no
        // relay TenantId field, so there is no second source to prefer over it).
        var tenantId = new ClaimsPrincipalAuthenticationContext(HttpContext.User).ActiveTenantId;

        var createResult = await _executionTracker.CreateItem(
            ExecutionItemTypes.Job,
            $"{pipelineName}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            parentId: null,
            correlationId: Guid.NewGuid().ToString(),
            triggerSource: triggerSource,
            parameters: new Dictionary<string, object?>
            {
                ["PipelineName"] = pipelineName
            },
            ct).ConfigureAwait(false);

        if (!createResult.IsSuccess)
        {
            return GenericResult<UnifiedTriggerResponse>.Failure(
                EtlServerLog.ExecutionRecordCreateFailed(_logger,
                    new InvalidOperationException(EtlServerLog.GetError(createResult, _logger)),
                    pipelineName,
                    EtlServerLog.GetError(createResult, _logger)));
        }

        var executionId = createResult.Value!.Id;

        var etlRecord = new EtlPipelineExecutionRecord
        {
            Id = executionId,
            PipelineName = pipelineName
        };
        var insertCommand = Insert.Into<EtlPipelineExecutionRecord>(EtlContainerName)
            .DataStore("EtlDb")
            .Path("etl")
            .Value(etlRecord);
        var insertResult = await _dataGateway.Execute<int>(insertCommand, ct).ConfigureAwait(false);
        if (!insertResult.IsSuccess)
        {
            EtlServerLog.EtlMetricsInsertFailed(_logger, executionId, pipelineName, insertResult.CurrentMessage);
        }

        var pipelineResult = await _pipelineProvider.Get(pipelineName, ct).ConfigureAwait(false);
        if (!pipelineResult.IsSuccess || pipelineResult.Value is null)
        {
            var errMsg = pipelineResult.CurrentMessage ?? $"Pipeline '{pipelineName}' not found";
            var completeResult = await _executionTracker.Complete(
                executionId, false, "PipelineNotFound", errMsg,
                CancellationToken.None).ConfigureAwait(false);
            if (!completeResult.IsSuccess)
            {
                EtlServerLog.ExecutionCompleteFailed(_logger, executionId, pipelineName, completeResult.CurrentMessage);
            }

            EtlServerLog.ProjectNotFoundForTrigger(_logger, pipelineName);
            return GenericResult<UnifiedTriggerResponse>.Failure(
                EtlServerLog.ProjectNotFoundForTrigger(_logger, pipelineName));
        }
        pipelineResult.Value.Dispose();

        EtlServerLog.PipelineExecutionQueued(_logger, pipelineName, executionId);
        await _broadcaster.BroadcastStatusChange(pipelineName, executionId, "Triggered").ConfigureAwait(false);

        var enqueued = await _pipelineQueue.Enqueue(new PipelineExecutionRequest
        {
            ExecutionId = executionId,
            PipelineName = pipelineName,
            TriggerSource = triggerSource,
            TenantId = tenantId
        }, ct).ConfigureAwait(false);

        if (!enqueued)
        {
            EtlLog.ExecutionQueueFull(_logger, pipelineName);
            return GenericResult<UnifiedTriggerResponse>.Success(new UnifiedTriggerResponse
            {
                ExecutionId = executionId,
                Status = "QueueFull"
            });
        }

        EtlLog.ExecutionEnqueued(_logger, pipelineName, executionId);
        return GenericResult<UnifiedTriggerResponse>.Success(new UnifiedTriggerResponse
        {
            ExecutionId = executionId,
            Status = "Queued"
        });
    }
}
