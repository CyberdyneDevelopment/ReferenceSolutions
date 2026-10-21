using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data.Extensions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Execution;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceEtlJobs.Endpoints.Logging;

namespace FdwReferenceEtlJobs.Endpoints;

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

    /// <summary>Gets or sets what caused the run: a registered JobTriggerSourceTypes member. Required (FDW-899).</summary>
    public string? TriggerSource { get; set; }

    /// <summary>
    /// Gets or sets the tenant relayed by the triggering caller (e.g. the scheduler, forwarding
    /// <c>ScheduleConfiguration.TenantId</c>). Only trusted when the caller's OWN authenticated tenant
    /// claim is absent — see <see cref="UnifiedTriggerEndpoint.TriggerPipeline"/>.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the run parameter values of a pipeline trigger (FDW-1110): each name must be a parameter the pipeline
    /// declares, each required parameter must be given, and each value is parsed by the declared type.
    /// </summary>
    public IList<PipelineParameterValue>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the roles a pipeline run acts with (FDW-1161), by name. Required for a pipeline: at least one, only roles the
    /// caller holds in the tenant it acts in, and the roles must be able to reach everything the pipeline reads and writes. A
    /// scheduled run relays the roles its schedule recorded.
    /// </summary>
    public IList<string>? RunRoles { get; set; }

    /// <summary>
    /// Gets or sets how many rows each source may read in a test run (FDW-1676). When set, the run is a test run: each source stops
    /// after this many rows, no destination is written and no step with an outside effect runs. When null, the run is a real run. Only a pipeline can be a test run.
    /// </summary>
    public int? TestRowsPerSource { get; set; }
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
    private readonly IExecutionTracker _executionTracker;
    private readonly ProjectExecutionQueue _projectQueue;
    private readonly IOrchestrationNodeConfigurationProvider _nodeProvider;
    private readonly IEffectivePolicyResolver _policyResolver;
    private readonly PipelineTrigger _pipelineTrigger;
    private readonly ILogger<UnifiedTriggerEndpoint> _logger;

    public UnifiedTriggerEndpoint(
        IExecutionTracker executionTracker,
        IDataGatewayProvider dataGateways,
        IEtlPipelineProvider pipelineProvider,
        IPipelineConfigurationProvider pipelineConfigurations,
        IAuthenticationContextAccessor accessor,
        IPipelineExecutionQueue pipelineQueue,
        IPipelineRunHolds holds,
        ProjectExecutionQueue projectQueue,
        IPipelineStatusBroadcaster broadcaster,
        IOrchestrationNodeConfigurationProvider nodeProvider,
        IRoleGrants grants,
        IDataSetConfigurationProvider dataSets,
        IConnectionConfigurationProvider connections,
        IDataStoreProvider dataStores,
        IEffectivePolicyResolver policyResolver,
        ILogger<UnifiedTriggerEndpoint> logger)
    {
        _executionTracker = executionTracker;
        _projectQueue = projectQueue;
        _nodeProvider = nodeProvider;
        _policyResolver = policyResolver;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<UnifiedTriggerEndpoint>.Instance;
        _pipelineTrigger = new PipelineTrigger(
            executionTracker, dataGateways, pipelineProvider, pipelineConfigurations, accessor, pipelineQueue, holds, broadcaster,
            new PipelineRunAuthority(grants, dataSets, connections, dataStores), _logger);
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
        // Why "!": the route template is "etl/trigger/{type}" and FastEndpoints' Route<T> is required
        // by default, so a request that reaches this handler always carries the segment.
        var type = Route<string>("type")!;
        EtlServerLog.UnifiedTriggerRequestReceived(_logger, type, req.Name, req.Id);

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
    /// Triggers a pipeline execution, for the caller of this request.
    /// </summary>
    public Task<IGenericResult<UnifiedTriggerResponse>> TriggerPipeline(
        UnifiedTriggerRequest req,
        CancellationToken ct = default) => _pipelineTrigger.Trigger(req, HttpContext.User, null, ct);

    private async Task<IGenericResult<UnifiedTriggerResponse>> TriggerProject(
        UnifiedTriggerRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.TriggerSource))
            return GenericResult<UnifiedTriggerResponse>.Failure(EtlServerLog.TriggerSourceMissing(_logger, req.Name, req.Id));

        // Why refused: a test run is a property of a pipeline run; a project that ignored it would run for real.
        if (req.TestRowsPerSource is not null)
            return GenericResult<UnifiedTriggerResponse>.Failure(EtlServerLog.TestRunOnlyForPipeline(_logger, req.Name, req.Id));

        var triggerSource = req.TriggerSource;

        // Resolve the project configuration to check approval policy. A name identifies one member of
        // the domain, so the read needs no parent to disambiguate it.
        // Why "!": HandleAsync refuses a request carrying neither Id nor Name before dispatching here.
        var projectResult = req.Id is { } projectId
            ? await _nodeProvider.Get(projectId, ct).ConfigureAwait(false)
            : await _nodeProvider.Get(req.Name!, ct).ConfigureAwait(false);

        if (!projectResult.IsSuccess || projectResult.Value is null)
        {
            return GenericResult<UnifiedTriggerResponse>.Failure(
                EtlServerLog.ProjectNotFoundForTrigger(_logger, req.Name, req.Id));
        }

        var project = projectResult.Value;
        var resolvedName = project.Name;

        // Why the same rule as a pipeline trigger: the caller's own tenant wins, and the body's is honoured
        // only for a caller whose token carries no tenant. The tracker refuses a run with neither.
        var createResult = await _executionTracker.CreateItem(
            ExecutionItemTypes.ByName("Project"),
            $"{resolvedName}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            new ClaimsPrincipalAuthenticationContext(HttpContext.User).ActiveTenantId ?? req.TenantId,
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

        // Why: if approval gate is set, hold in AwaitingApproval — do NOT enqueue. The run is moved to that state (FDW-1740), so a waiting run is
        // told from any other new run, and only a decision by someone who holds pipelines:approve starts it. A hold that cannot be recorded is a
        // failure: answering AwaitingApproval for a run that is not marked as waiting would leave it waiting for nobody.
        // The project's own setting wins; a project that states none inherits the server default (a null column means inherit).
        if (_policyResolver.ResolveForRootNode(project).RequireApprovalToRun)
        {
            var held = await _executionTracker.TransitionState(
                executionId, ExecutionStateTypes.AwaitingApproval, "Held for approval", "ApprovalGate", ct).ConfigureAwait(false);
            if (held.IsFailure)
            {
                return GenericResult<UnifiedTriggerResponse>.Failure(
                    EtlServerLog.ApprovalHoldFailed(_logger, executionId, resolvedName, held.CurrentMessage));
            }

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
