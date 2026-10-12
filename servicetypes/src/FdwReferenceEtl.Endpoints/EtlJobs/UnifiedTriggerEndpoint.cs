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
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IEtlPipelineProvider _pipelineProvider;
    private readonly IPipelineConfigurationProvider _pipelineConfigurations;
    private readonly IAuthenticationContextAccessor _accessor;
    private readonly IPipelineExecutionQueue _pipelineQueue;
    private readonly ProjectExecutionQueue _projectQueue;
    private readonly IPipelineStatusBroadcaster _broadcaster;
    private readonly IOrchestrationNodeConfigurationProvider _nodeProvider;
    private readonly PipelineRunAuthority _runAuthority;
    private readonly ILogger<UnifiedTriggerEndpoint> _logger;

    public UnifiedTriggerEndpoint(
        IExecutionTracker executionTracker,
        IDataGatewayProvider dataGateways,
        IEtlPipelineProvider pipelineProvider,
        IPipelineConfigurationProvider pipelineConfigurations,
        IAuthenticationContextAccessor accessor,
        IPipelineExecutionQueue pipelineQueue,
        ProjectExecutionQueue projectQueue,
        IPipelineStatusBroadcaster broadcaster,
        IOrchestrationNodeConfigurationProvider nodeProvider,
        IRoleGrants grants,
        IDataSetConfigurationProvider dataSets,
        IConnectionConfigurationProvider connections,
        IDataStoreProvider dataStores,
        ILogger<UnifiedTriggerEndpoint> logger)
    {
        _executionTracker = executionTracker;
        _dataGateways = dataGateways;
        _pipelineProvider = pipelineProvider;
        _pipelineConfigurations = pipelineConfigurations;
        _accessor = accessor;
        _pipelineQueue = pipelineQueue;
        _projectQueue = projectQueue;
        _broadcaster = broadcaster;
        _nodeProvider = nodeProvider;
        _runAuthority = new PipelineRunAuthority(grants, dataSets, connections, dataStores);
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
    /// Triggers a pipeline execution.
    /// </summary>
    public async Task<IGenericResult<UnifiedTriggerResponse>> TriggerPipeline(
        UnifiedTriggerRequest req,
        CancellationToken ct = default)
    {
        // Why no defaults (FDW-899): a pipeline is named -- by exactly one of its id or its name (FDW-1088) -- and
        // what caused the run is recorded on the execution; neither is guessed. This method is also reached without
        // the request validators, so it refuses them itself.
        var byName = !string.IsNullOrWhiteSpace(req.Name);
        if (byName == req.Id.HasValue)
            return GenericResult<UnifiedTriggerResponse>.Failure(EtlServerLog.PipelineNameRequiredForTrigger(_logger));

        if (string.IsNullOrWhiteSpace(req.TriggerSource))
            return GenericResult<UnifiedTriggerResponse>.Failure(EtlServerLog.TriggerSourceMissing(_logger, req.Name, req.Id));

        // What the run is called until the pipeline is resolved: its name, or its id when it was named by id.
        var pipelineName = byName ? req.Name! : req.Id!.Value.ToString("D", CultureInfo.InvariantCulture);
        var triggerSource = req.TriggerSource;

        // Why the caller's own claim wins over the request-body TenantId: an authenticated per-tenant
        // caller's token always carries their own tenant_id, so trusting a body-supplied TenantId
        // directly would let them request execution scoped to a DIFFERENT tenant (cross-tenant RLS
        // escalation). The body field is only honored when the caller's own token carries no tenant
        // scope — the case for a system/service-account caller (e.g. the scheduler, which dispatches
        // for every tenant's schedules and so has no single tenant identity of its own).
        var callerTenant = new ClaimsPrincipalAuthenticationContext(HttpContext.User).ActiveTenantId;
        var tenantId = callerTenant ?? req.TenantId;

        // The execution record belongs to the run's tenant; the tracker refuses a run with none.
        var createResult = await _executionTracker.CreateItem(
            ExecutionItemTypes.Job,
            $"{pipelineName}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            tenantId,
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
        // Why no StartedAt: a queued run has not started. The executor sets it when the run reaches Running.
        var etlRecord = new EtlPipelineExecutionRecord
        {
            Id = executionId,
            PipelineName = pipelineName,
            Status = createResult.Value!.State.Name
        };
        var operationalStore = EtlPipelineTypes.OperationalConnection;
        if (string.IsNullOrWhiteSpace(operationalStore))
        {
            // Reported the same way a failed insert is, because this write is already non-fatal.
            EtlServerLog.EtlMetricsInsertFailed(
                _logger,
                executionId,
                pipelineName,
                $"{nameof(EtlPipelineTypes)}.{nameof(EtlPipelineTypes.OperationalConnection)} is not set; the host must name the operational store.");
        }
        else
        {
            var insertCommand = Insert.Into<EtlPipelineExecutionRecord>(EtlContainerName)
                .DataStore(operationalStore)
                .Path("etl")
                .Value(etlRecord);
            var insertResult = await _dataGateways.ByName("Main").Execute<int>(insertCommand, ct).ConfigureAwait(false);
            if (!insertResult.IsSuccess)
            {
                EtlServerLog.EtlMetricsInsertFailed(_logger, executionId, pipelineName, insertResult.CurrentMessage);
            }
        }

        // Resolved by name in the caller's view -- or, for a service caller relaying a tenant, in that
        // tenant's -- then built by the id that resolution found: the worker runs exactly that pipeline,
        // never another tenant's of the same name (FDW-1087).
        var relayedTenant = callerTenant is null ? tenantId : null;
        var (resolvedId, resolvedName, notFoundReason, declared, reach) = await ResolvePipeline(
            byName ? req.Name : null, byName ? null : req.Id, relayedTenant, ct).ConfigureAwait(false);
        if (resolvedId is not { } pipelineId)
            return await RefusePipelineNotFound(executionId, pipelineName, notFoundReason).ConfigureAwait(false);
        pipelineName = resolvedName!;

        // The roles the run acts with are the run's own (FDW-1161), judged now for everything the pipeline reaches: a caller
        // names roles it holds; a schedule's firing, which has no person behind it, relays the roles its schedule recorded.
        var authorized = await AuthorizeRun(req, reach!, callerTenant is null, relayedTenant, ct).ConfigureAwait(false);
        if (authorized.IsFailure)
            return await RefuseRun(executionId, pipelineName, "PipelineRunRefused", authorized).ConfigureAwait(false);

        // The values a run is given are judged against what the pipeline declares before it is queued: an undeclared name, a
        // missing required parameter and a value its declared type cannot parse are refused with the parameter named.
        var values = PipelineRunParameterValues.Resolve(declared, SuppliedValues(req), _logger);
        if (values.IsFailure)
            return await RefuseRun(executionId, pipelineName, "InvalidRunParameters", values).ConfigureAwait(false);

        EtlServerLog.PipelineExecutionQueued(_logger, pipelineName, executionId);
        await _broadcaster.BroadcastStatusChange(pipelineName, executionId, "Triggered").ConfigureAwait(false);

        var enqueued = await _pipelineQueue.Enqueue(new PipelineExecutionRequest
        {
            ExecutionId = executionId,
            PipelineId = pipelineId,
            PipelineName = pipelineName,
            TriggerSource = triggerSource,
            RunRoles = RunRoleSelection.Chosen(req.RunRoles),
            TenantId = tenantId,
            Parameters = values.Value
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
        if (string.IsNullOrWhiteSpace(req.TriggerSource))
            return GenericResult<UnifiedTriggerResponse>.Failure(EtlServerLog.TriggerSourceMissing(_logger, req.Name, req.Id));

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

    // The pipeline, resolved by the id or the name it was named by and built in the view the trigger is entitled
    // to: its caller's own, or the relayed tenant's for a service caller with none. A schedule relays the id
    // (FDW-1088), so it resolves exactly the pipeline it stores, never another tenant's of the same name.
    private async Task<(Guid? Id, string? Name, string? NotFoundReason, IList<PipelineRunParameterConfiguration>? Declared, PipelineReach? Reach)> ResolvePipeline(
        string? pipelineName, Guid? pipelineId, Guid? relayedTenant, CancellationToken ct)
    {
        using var view = new RelayedTenantView(_accessor, relayedTenant);
        var configuration = pipelineId is { } id
            ? await _pipelineConfigurations.Get(id, ct).ConfigureAwait(false)
            : await _pipelineConfigurations.Get(pipelineName!, ct).ConfigureAwait(false);
        if (!configuration.IsSuccess || configuration.Value is null)
            return (null, null, configuration.CurrentMessage, null, null);

        var pipeline = await _pipelineProvider.Get(configuration.Value.Id, ct).ConfigureAwait(false);
        if (!pipeline.IsSuccess || pipeline.Value is null)
            return (null, null, pipeline.CurrentMessage, null, null);
        pipeline.Value.Dispose();
        var declared = configuration.Value is IPipelineLoadConfiguration load ? load.RunParameters : null;
        return (configuration.Value.Id, configuration.Value.Name, null, declared, PipelineReach.Of(PipelineDetailMapper.Map(configuration.Value)));
    }

    // The roles judged for what the pipeline reaches, in the view the trigger is entitled to: the caller's own, or the relayed
    // tenant's for a service caller with none, so what is visible to the run is what would be visible to its tenant.
    private async Task<IGenericResult> AuthorizeRun(
        UnifiedTriggerRequest req, PipelineReach reach, bool relayed, Guid? relayedTenant, CancellationToken ct)
    {
        using var view = new RelayedTenantView(_accessor, relayedTenant);
        return relayed
            ? await _runAuthority.AuthorizeScheduled(reach, RunRoleSelection.Chosen(req.RunRoles), _logger, ct).ConfigureAwait(false)
            : await _runAuthority.Authorize(
                new ClaimsPrincipalAuthenticationContext(HttpContext.User), reach, RunRoleSelection.Chosen(req.RunRoles), _logger, ct).ConfigureAwait(false);
    }

    private static List<KeyValuePair<string, string>> SuppliedValues(UnifiedTriggerRequest req) =>
        req.Parameters is null ? [] : [.. req.Parameters.Select(p => new KeyValuePair<string, string>(p.Name, p.Value))];

    // The run record already exists, so a run refused for what it was given closes it as failed with the reason.
    private async Task<IGenericResult<UnifiedTriggerResponse>> RefuseRun(
        Guid executionId, string pipelineName, string code, IGenericResult refusal)
    {
        var completeResult = await _executionTracker.Complete(
            executionId, false, code, refusal.CurrentMessage, CancellationToken.None).ConfigureAwait(false);
        if (!completeResult.IsSuccess)
        {
            EtlServerLog.ExecutionCompleteFailed(_logger, executionId, pipelineName, completeResult.CurrentMessage);
        }

        return refusal.ToNewResult<UnifiedTriggerResponse>();
    }

    // The run record already exists, so a pipeline the caller cannot resolve closes it as not found.
    private async Task<IGenericResult<UnifiedTriggerResponse>> RefusePipelineNotFound(
        Guid executionId, string pipelineName, string? reason)
    {
        var notFound = EtlServerLog.PipelineNotFoundForTrigger(_logger, pipelineName, reason);
        var completeResult = await _executionTracker.Complete(
            executionId, false, "PipelineNotFound", notFound.Message,
            CancellationToken.None).ConfigureAwait(false);
        if (!completeResult.IsSuccess)
        {
            EtlServerLog.ExecutionCompleteFailed(_logger, executionId, pipelineName, completeResult.CurrentMessage);
        }

        return GenericResult<UnifiedTriggerResponse>.Failure(notFound);
    }
}
