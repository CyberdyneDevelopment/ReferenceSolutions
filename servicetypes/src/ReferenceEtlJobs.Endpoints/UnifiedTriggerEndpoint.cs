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
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Execution;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Notifications;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceEtlJobs.Endpoints.Logging;

namespace ReferenceEtlJobs.Endpoints;

/// <summary>
/// Request body for the unified trigger endpoint.
/// Resolve target by <see cref="Id"/> OR by (<see cref="Name"/> + optional <see cref="ParentPath"/>).
/// For globally-unique types (pipeline, project) <see cref="ParentPath"/> is ignored.
/// For non-globally-unique types (stage, step) <see cref="ParentPath"/> disambiguates.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UnifiedTriggerRequest
{
    /// <summary>Gets or sets the logical identifier of the target. If set, name/parentPath are ignored.</summary>
    public Guid? Id { get; set; }

    /// <summary>Gets or sets the name of the target. Required when Id is not provided.</summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the parent path (e.g. "MyProject/Stage1") needed to disambiguate
    /// types that are not globally unique by name.
    /// </summary>
    public string? ParentPath { get; set; }

    /// <summary>Gets or sets the trigger source label (e.g. "Api", "Scheduler").</summary>
    public string? TriggerSource { get; set; }

    /// <summary>
    /// Gets or sets the tenant relayed by the triggering caller (e.g. the scheduler, forwarding
    /// <c>ScheduleConfiguration.TenantId</c>). Only trusted when the caller's OWN authenticated tenant
    /// claim is absent — see <see cref="UnifiedTriggerEndpoint.TriggerPipeline"/>.
    /// </summary>
    public Guid? TenantId { get; set; }
}

/// <summary>
/// Response returned by the unified trigger endpoint (and by the deprecated alias).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UnifiedTriggerResponse
{
    /// <summary>Gets or sets the execution item ID for tracking progress.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the initial status (e.g. "Queued", "AwaitingApproval", "QueueFull").</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// <c>POST /etl/trigger/{type}</c> — unified trigger for any registered execution item type.
///
/// <para>
/// <c>type</c> is resolved via <c>ExecutionItemTypes.ByName(type)</c>. This means any new type
/// registered as a <c>[TypeOption]</c> on <c>ExecutionItemTypes</c> is automatically supported
/// without changes to this endpoint. Current registered types: pipeline, project, stage, step, task,
/// job, workflow, test.
/// </para>
///
/// <para>
/// Approval gate: if the resolved effective policy has <c>RequireApprovalToRun=true</c>, the
/// execution item is created in <c>AwaitingApproval</c> state and NOT enqueued. The caller
/// receives <c>{ executionId, status: "AwaitingApproval" }</c> and must subsequently call
/// <c>POST /etl/executions/{id}/approve</c>.
/// </para>
///
/// <para>
/// Partial execution (stage/step): not supported in v1. Returns 501 with a clear error message.
/// </para>
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UnifiedTriggerEndpoint : Endpoint<UnifiedTriggerRequest, UnifiedTriggerResponse>
{
    private const string EtlContainerName = "PipelineExecution";

    private readonly IExecutionTracker _executionTracker;
    private readonly IDataGateway _dataGateway;
    private readonly IFdwServiceProvider<IEtlPipeline, PipelineConfiguration> _pipelineProvider;
    private readonly IPipelineExecutionQueue _pipelineQueue;
    private readonly ProjectExecutionQueue _projectQueue;
    private readonly IPipelineStatusBroadcaster _broadcaster;
    private readonly IOrchestrationNodeConfigurationProvider _nodeProvider;
    private readonly ILogger<UnifiedTriggerEndpoint> _logger;

    public UnifiedTriggerEndpoint(
        IExecutionTracker executionTracker,
        IDataGateway dataGateway,
        IFdwServiceProvider<IEtlPipeline, PipelineConfiguration> pipelineProvider,
        IPipelineExecutionQueue pipelineQueue,
        ProjectExecutionQueue projectQueue,
        IPipelineStatusBroadcaster broadcaster,
        IOrchestrationNodeConfigurationProvider nodeProvider,
        ILogger<UnifiedTriggerEndpoint> logger)
    {
        _executionTracker = executionTracker;
        _dataGateway = dataGateway;
        _pipelineProvider = pipelineProvider;
        _pipelineQueue = pipelineQueue;
        _projectQueue = projectQueue;
        _broadcaster = broadcaster;
        _nodeProvider = nodeProvider;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<UnifiedTriggerEndpoint>.Instance;
    }

    public override void Configure()
    {
        Post("etl/trigger/{type}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:execute");
#endif
        Summary(s =>
        {
            s.Summary = "Unified ETL trigger";
            s.Description = "Triggers execution of a pipeline, project, stage, step, or any registered execution type. " +
                             "Type is an open TypeCollection lookup — new types are supported without code changes. " +
                             "Body: { id?, name?, parentPath?, triggerSource? }. " +
                             "Requires id OR name (+parentPath for non-globally-unique types). " +
                             "When RequireApprovalToRun is true, returns status=AwaitingApproval.";
            s.ExampleRequest = new UnifiedTriggerRequest
            {
                Name = "NflDataProject",
                TriggerSource = "Api"
            };
        });
    }

    public override async Task HandleAsync(UnifiedTriggerRequest req, CancellationToken ct)
    {
        var type = Route<string>("type") ?? string.Empty;
        var displayName = req.Name ?? req.Id?.ToString() ?? "(unknown)";
        EtlServerLog.UnifiedTriggerRequestReceived(_logger, type, displayName);

        // Why: normalize to TitleCase before ByName lookup because ExecutionItemTypes names are
        // TitleCase ("Stage", "Pipeline") but URL route parameters arrive in lowercase ("stage",
        // "pipeline"). ByName() is case-sensitive (FrozenDictionary with Ordinal comparer).
        var normalizedType = type.Length > 0
            ? char.ToUpperInvariant(type[0]) + type[1..].ToLowerInvariant()
            : type;

        // Why: resolve via TypeCollection lookup rather than a switch. Any new [TypeOption]
        // registered on ExecutionItemTypes is automatically routed here without code changes.
        var itemType = ExecutionItemTypes.ByName(normalizedType);
        if (itemType == ExecutionItemTypes.NotFound)
        {
            EtlServerLog.UnknownTriggerType(_logger, type);
            AddError(EtlServerLog.GetError(GenericResult.Failure(
                EtlServerLog.UnknownTriggerType(_logger, type)), _logger));
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        // Why: validate that id OR name is present
        if (req.Id is null && string.IsNullOrWhiteSpace(req.Name))
        {
            AddError("Either 'id' or 'name' must be provided.");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        // Why: dispatch based on TypeCollection name (string comparison only at routing layer).
        // All internal logic after this point treats executions generically via IExecutionTracker.
        if (string.Equals(itemType.Name, "pipeline", StringComparison.OrdinalIgnoreCase))
        {
            var result = await TriggerPipeline(req, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AddError(EtlServerLog.GetError(result, _logger));
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
                return;
            }

            var r = result.Value!;
            var status = string.Equals(r.Status, "QueueFull", StringComparison.Ordinal) ? 503 : 202;
            await Send.ResponseAsync(r, status, ct).ConfigureAwait(false);
            return;
        }

        if (string.Equals(itemType.Name, "project", StringComparison.OrdinalIgnoreCase))
        {
            var result = await TriggerProject(req, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AddError(EtlServerLog.GetError(result, _logger));
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
                return;
            }

            var r = result.Value!;
            // Why: AwaitingApproval → 202 with status field; QueueFull → 503
            var status = string.Equals(r.Status, "QueueFull", StringComparison.Ordinal) ? 503 : 202;
            await Send.ResponseAsync(r, status, ct).ConfigureAwait(false);
            return;
        }

        // Why: stage/step/task partial execution is not supported in v1. Return 501 per plan decision.
        // This response is intentional and documents the gap rather than silently failing.
        await Send.ResponseAsync(new UnifiedTriggerResponse
        {
            ExecutionId = Guid.Empty,
            Status = "NotImplemented"
        }, 501, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Triggers a pipeline execution. Called from both the unified endpoint and the
    /// deprecated <see cref="TriggerJobEndpoint"/> alias to avoid logic duplication.
    /// </summary>
    public async Task<IGenericResult<UnifiedTriggerResponse>> TriggerPipeline(
        UnifiedTriggerRequest req,
        CancellationToken ct = default)
    {
        var pipelineName = req.Name ?? string.Empty;
        var triggerSource = req.TriggerSource ?? "Api";

        // Why the caller's own claim wins over the request-body TenantId: an authenticated per-tenant
        // caller's token always carries their own tenant_id, so trusting a body-supplied TenantId
        // directly would let them request execution scoped to a DIFFERENT tenant (cross-tenant RLS
        // escalation). The body field is only honored when the caller's own token carries no tenant
        // scope — the case for a system/service-account caller (e.g. the scheduler, which dispatches
        // for every tenant's schedules and so has no single tenant identity of its own).
        var tenantId = new ClaimsPrincipalAuthenticationContext(HttpContext.User).ActiveTenantId ?? req.TenantId;

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

        // Insert ETL-specific metrics record (non-fatal if it fails)
        var etlRecord = new EtlPipelineExecutionRecord
        {
            Id = executionId,
            PipelineName = pipelineName
        };
        var insertCommand = Insert.Into<EtlPipelineExecutionRecord>(EtlContainerName)
            .DataStore("OpsDb")
            .Path("etl")
            .Value(etlRecord);
        var insertResult = await _dataGateway.Execute<int>(insertCommand, ct).ConfigureAwait(false);
        if (!insertResult.IsSuccess)
        {
            EtlServerLog.EtlMetricsInsertFailed(_logger, executionId, pipelineName, insertResult.CurrentMessage);
        }

        // Validate pipeline exists before enqueuing
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

    private async Task<IGenericResult<UnifiedTriggerResponse>> TriggerProject(
        UnifiedTriggerRequest req,
        CancellationToken ct)
    {
        var projectName = req.Name ?? string.Empty;
        var triggerSource = req.TriggerSource ?? "Api";

        // Resolve the project configuration to check approval policy.
        // Why parentId: null — projects are root-level nodes with no parent.
        var projectResult = req.Id.HasValue
            ? await _nodeProvider.Get(req.Id.Value, ct).ConfigureAwait(false)
            : await _nodeProvider.Get(projectName, parentId: null, ct).ConfigureAwait(false);

        if (!projectResult.IsSuccess || projectResult.Value is null)
        {
            EtlServerLog.ProjectNotFoundForTrigger(_logger, projectName);
            return GenericResult<UnifiedTriggerResponse>.Failure(
                EtlServerLog.ProjectNotFoundForTrigger(_logger, projectName));
        }

        var project = projectResult.Value;
        var resolvedName = project.Name;

        var createResult = await _executionTracker.CreateItem(
            ExecutionItemTypes.ByName("Project"),
            $"{resolvedName}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            parentId: null,
            correlationId: Guid.NewGuid().ToString(),
            triggerSource: triggerSource,
            parameters: new Dictionary<string, object?>
            {
                ["ProjectName"] = resolvedName,
                ["ProjectId"] = project.Id.ToString()
            },
            ct).ConfigureAwait(false);

        if (!createResult.IsSuccess)
        {
            return GenericResult<UnifiedTriggerResponse>.Failure(
                EtlServerLog.ExecutionRecordCreateFailed(_logger,
                    new InvalidOperationException(EtlServerLog.GetError(createResult, _logger)),
                    resolvedName,
                    EtlServerLog.GetError(createResult, _logger)));
        }

        var executionId = createResult.Value!.Id;

        // Why: if approval gate is set, hold in AwaitingApproval — do NOT enqueue
        if (project.RequireApprovalToRun == true)
        {
            EtlServerLog.ExecutionAwaitingApproval(_logger, executionId, resolvedName);
            return GenericResult<UnifiedTriggerResponse>.Success(new UnifiedTriggerResponse
            {
                ExecutionId = executionId,
                Status = "AwaitingApproval"
            });
        }

        var enqueued = await _projectQueue.Enqueue(new ProjectExecutionRequest
        {
            ExecutionId = executionId,
            ProjectName = resolvedName,
            TriggerSource = triggerSource
        }, ct).ConfigureAwait(false);

        if (!enqueued)
        {
            EtlServerLog.UnifiedTriggerQueueFull(_logger, "project", resolvedName);
            return GenericResult<UnifiedTriggerResponse>.Success(new UnifiedTriggerResponse
            {
                ExecutionId = executionId,
                Status = "QueueFull"
            });
        }

        EtlServerLog.ProjectExecutionQueued(_logger, resolvedName, executionId);
        return GenericResult<UnifiedTriggerResponse>.Success(new UnifiedTriggerResponse
        {
            ExecutionId = executionId,
            Status = "Queued"
        });
    }
}
