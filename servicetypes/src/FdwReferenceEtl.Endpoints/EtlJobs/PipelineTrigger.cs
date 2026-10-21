using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Extensions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Notifications;
using Microsoft.Extensions.Logging;
using FdwReferenceEtlJobs.Endpoints.Logging;

namespace FdwReferenceEtlJobs.Endpoints;

/// <summary>
/// Starts one run of a stored pipeline (FDW-1884: moved out of the unified trigger endpoint so a webhook trigger starts a run by the
/// same code): records the run, resolves the pipeline, judges the run roles for what the pipeline reaches, resolves the parameter
/// values and queues the run.
/// </summary>
/// <remarks>
/// Why the caller is a parameter and not read from the request: a person's call carries their own claims, whose tenant wins over the
/// request's; a call with no person behind it (the scheduler, a webhook) carries none, and then the tenant the request relays is the
/// one the run acts in, judged as a scheduled run is. Not registered in the container: the two endpoints that start runs build it from
/// the services they are given.
/// </remarks>
internal sealed class PipelineTrigger
{
    private const string EtlContainerName = "PipelineExecution";

    private readonly IExecutionTracker _executionTracker;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IEtlPipelineProvider _pipelineProvider;
    private readonly IPipelineConfigurationProvider _pipelineConfigurations;
    private readonly IAuthenticationContextAccessor _accessor;
    private readonly IPipelineExecutionQueue _pipelineQueue;
    private readonly IPipelineRunHolds _holds;
    private readonly IPipelineStatusBroadcaster _broadcaster;
    private readonly PipelineRunAuthority _runAuthority;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineTrigger"/> class.</summary>
    /// <param name="executionTracker">Records the run.</param>
    /// <param name="dataGateways">Writes the run's metrics row.</param>
    /// <param name="pipelineProvider">Builds the pipeline, to prove it can be built.</param>
    /// <param name="pipelineConfigurations">Reads the pipeline.</param>
    /// <param name="accessor">The ambient caller, swapped for the relayed tenant's while the pipeline is resolved.</param>
    /// <param name="pipelineQueue">Queues the run.</param>
    /// <param name="holds">Keeps the request of a run that waits for approval.</param>
    /// <param name="broadcaster">Announces the run to subscribers.</param>
    /// <param name="runAuthority">Judges the run roles for what the pipeline reaches.</param>
    /// <param name="logger">The logger.</param>
    public PipelineTrigger(
        IExecutionTracker executionTracker,
        IDataGatewayProvider dataGateways,
        IEtlPipelineProvider pipelineProvider,
        IPipelineConfigurationProvider pipelineConfigurations,
        IAuthenticationContextAccessor accessor,
        IPipelineExecutionQueue pipelineQueue,
        IPipelineRunHolds holds,
        IPipelineStatusBroadcaster broadcaster,
        PipelineRunAuthority runAuthority,
        ILogger logger)
    {
        _executionTracker = executionTracker;
        _dataGateways = dataGateways;
        _pipelineProvider = pipelineProvider;
        _pipelineConfigurations = pipelineConfigurations;
        _accessor = accessor;
        _pipelineQueue = pipelineQueue;
        _holds = holds;
        _broadcaster = broadcaster;
        _runAuthority = runAuthority;
        _logger = logger;
    }

    /// <summary>Starts a pipeline run.</summary>
    /// <param name="req">What to start: the pipeline by id or name, the trigger source, the tenant to relay, the parameters, the run roles.</param>
    /// <param name="caller">The principal of the call; one with no tenant makes the run act in the tenant the request relays.</param>
    /// <param name="webhook">The name of the webhook trigger that was called, recorded on the run; null for a run no webhook started.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The run and its status, or the refusal.</returns>
    public async Task<IGenericResult<UnifiedTriggerResponse>> Trigger(UnifiedTriggerRequest req, ClaimsPrincipal caller, string? webhook, CancellationToken ct)
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

        // Why the caller's own claim wins over the request-body TenantId: an authenticated per-tenant
        // caller's token always carries their own tenant_id, so trusting a body-supplied TenantId
        // directly would let them request execution scoped to a DIFFERENT tenant (cross-tenant RLS
        // escalation). The body field is only honored when the caller's own token carries no tenant
        // scope — the case for a system/service-account caller (e.g. the scheduler, which dispatches
        // for every tenant's schedules and so has no single tenant identity of its own).
        var callerTenant = new ClaimsPrincipalAuthenticationContext(caller).ActiveTenantId;
        var tenantId = callerTenant ?? req.TenantId;

        var relayedTenant = callerTenant is null ? tenantId : null;
        var resolution = await ResolvePipeline(byName ? req.Name : null, byName ? null : req.Id, relayedTenant, ct).ConfigureAwait(false);

        // Why the run is named after the pipeline once it is found: a run is listed under its pipeline by that name, and a run started by id
        // (a schedule, a webhook) is named like every other. Only a pipeline that cannot be found keeps the id it was asked for.
        if (resolution.Name is { } resolvedName)
            pipelineName = resolvedName;

        // The execution record belongs to the run's tenant; the tracker refuses a run with none.
        var createResult = await _executionTracker.CreateItem(
            ExecutionItemTypes.Job,
            $"{pipelineName}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            tenantId,
            parentId: null,
            correlationId: Guid.NewGuid().ToString(),
            triggerSource: req.TriggerSource,
            parameters: TriggerFacts(pipelineName, req.TestRowsPerSource, webhook),
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
        await InsertMetrics(executionId, pipelineName, createResult.Value!.State.Name, ct).ConfigureAwait(false);
        return await Queue(req, caller, resolution, pipelineName, callerTenant, tenantId, relayedTenant, executionId, ct).ConfigureAwait(false);
    }

    // Insert ETL-specific metrics record (non-fatal if it fails)
    // Why no StartedAt: a queued run has not started. The executor sets it when the run reaches Running.
    private async Task InsertMetrics(Guid executionId, string pipelineName, string status, CancellationToken ct)
    {
        var etlRecord = new EtlPipelineExecutionRecord
        {
            Id = executionId,
            PipelineName = pipelineName,
            Status = status
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
            return;
        }

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

    // Resolves the values, judges the reach and the roles and queues the run, closing the run record as failed when it is refused.
    private async Task<IGenericResult<UnifiedTriggerResponse>> Queue(
        UnifiedTriggerRequest req, ClaimsPrincipal caller, Resolution resolution, string pipelineName, Guid? callerTenant, Guid? tenantId,
        Guid? relayedTenant, Guid executionId, CancellationToken ct)
    {
        var (resolvedId, _, notFoundReason, declared, graph) = resolution;
        if (resolvedId is not { } pipelineId || graph is null)
            return await RefusePipelineNotFound(executionId, pipelineName, notFoundReason).ConfigureAwait(false);

        // The values a run is given are judged against what the pipeline declares before it is queued: an undeclared name, a
        // missing required parameter and a value its declared type cannot parse are refused with the parameter named. They come
        // before the roles are judged because what the run reaches can depend on them (FDW-1891): a source may name its data set by a parameter.
        var values = PipelineRunParameterValues.Resolve(declared, SuppliedValues(req), _logger);
        if (values.IsFailure || values.Value is null)
            return await RefuseRun(executionId, pipelineName, "InvalidRunParameters", values).ConfigureAwait(false);

        // What the run reaches, read from the pipeline's components rendered with those values exactly as the run builds them.
        var reach = PipelineReach.Of(graph, values.Value, _logger);
        if (reach.IsFailure || reach.Value is null)
            return await RefuseRun(executionId, pipelineName, "PipelineRunRefused", reach).ConfigureAwait(false);

        // The roles the run acts with are the run's own (FDW-1161), judged now for everything the pipeline reaches: a caller
        // names roles it holds; a schedule's firing, which has no person behind it, relays the roles its schedule recorded.
        var authorized = await AuthorizeRun(req, caller, reach.Value, callerTenant is null, relayedTenant, ct).ConfigureAwait(false);
        if (authorized.IsFailure)
            return await RefuseRun(executionId, pipelineName, "PipelineRunRefused", authorized).ConfigureAwait(false);

        await RecordGivenParameters(
            executionId, pipelineName, declared, values.Value, PipelineRunParameterValues.Defaulted(declared, SuppliedValues(req).Select(supplied => supplied.Key)), ct).ConfigureAwait(false);

        var request = RunRequest(req, executionId, pipelineId, pipelineName, tenantId, values.Value);

        // A pipeline that requires approval is judged like any other and then waits instead of being queued (FDW-1937).
        if (graph.RequireApprovalToRun)
        {
            return await HoldForApproval(request, ct).ConfigureAwait(false);
        }

        EtlServerLog.PipelineExecutionQueued(_logger, pipelineName, executionId);
        await _broadcaster.BroadcastStatusChange(pipelineName, executionId, "Triggered").ConfigureAwait(false);

        var enqueued = await _pipelineQueue.Enqueue(request, ct).ConfigureAwait(false);

        if (!enqueued)
        {
            EtlLog.ExecutionQueueFull(_logger, pipelineName);
            return GenericResult<UnifiedTriggerResponse>.Success(new UnifiedTriggerResponse { ExecutionId = executionId, Status = "QueueFull" });
        }

        EtlLog.ExecutionEnqueued(_logger, pipelineName, executionId);
        return GenericResult<UnifiedTriggerResponse>.Success(new UnifiedTriggerResponse { ExecutionId = executionId, Status = "Queued" });
    }

    // What the worker needs to run it: the pipeline the trigger resolved, the roles it acts with and the values it was given.
    private static PipelineExecutionRequest RunRequest(
        UnifiedTriggerRequest req, Guid executionId, Guid pipelineId, string pipelineName, Guid? tenantId, IReadOnlyDictionary<string, object?> values) => new()
    {
        ExecutionId = executionId,
        PipelineId = pipelineId,
        PipelineName = pipelineName,
        TriggerSource = req.TriggerSource!,
        RunRoles = RunRoleSelection.Chosen(req.RunRoles),
        TenantId = tenantId,
        Parameters = values,
        TestRowsPerSource = req.TestRowsPerSource
    };

    // Keeps the request that would have been queued and moves the run to AwaitingApproval, so a waiting run is told from any other new run and
    // only a decision by someone who holds pipelines:approve queues it. A hold that cannot be recorded fails the start: answering
    // AwaitingApproval for a run that is not marked as waiting would leave it waiting for nobody.
    private async Task<IGenericResult<UnifiedTriggerResponse>> HoldForApproval(PipelineExecutionRequest request, CancellationToken ct)
    {
        if (!_holds.Hold(request))
        {
            return GenericResult<UnifiedTriggerResponse>.Failure(EtlServerLog.PipelineHoldRefused(_logger, request.ExecutionId, request.PipelineName));
        }

        var held = await _executionTracker.TransitionState(
            request.ExecutionId, ExecutionStateTypes.AwaitingApproval, "Held for approval", "ApprovalGate", ct).ConfigureAwait(false);
        if (held.IsFailure)
        {
            _ = _holds.Take(request.ExecutionId);
            return GenericResult<UnifiedTriggerResponse>.Failure(
                EtlServerLog.PipelineApprovalHoldFailed(_logger, request.ExecutionId, request.PipelineName, held.CurrentMessage));
        }

        EtlServerLog.PipelineHeldForApproval(_logger, request.ExecutionId, request.PipelineName);
        await _broadcaster.BroadcastStatusChange(request.PipelineName, request.ExecutionId, "AwaitingApproval").ConfigureAwait(false);
        return GenericResult<UnifiedTriggerResponse>.Success(new UnifiedTriggerResponse { ExecutionId = request.ExecutionId, Status = "AwaitingApproval" });
    }

    // What the run was given, on its record (FDW-1756): each declared parameter it was given, with the value only for one whose author opted in; a
    // parameter that took the default its pipeline declares is recorded as not given (FDW-1975). It
    // is recorded as an event because the record itself is written before the values exist, and its failure is logged, not fatal: the run is
    // already accepted and the entries only describe it.
    private async Task RecordGivenParameters(
        Guid executionId, string pipelineName, IReadOnlyList<IPipelineRunParameter>? declared, IReadOnlyDictionary<string, object?> values,
        IReadOnlySet<string> defaulted, CancellationToken ct)
    {
        if (RunParameterEntries.EventData(declared, values, defaulted) is not { } data)
            return;

        var recorded = await _executionTracker.RecordEvent(
            executionId, RunParameterEntries.EventType, "Parameters the run was given", data, cancellationToken: ct).ConfigureAwait(false);
        if (!recorded.IsSuccess)
            EtlServerLog.RunParametersRecordFailed(_logger, executionId, pipelineName, recorded.CurrentMessage);
    }

    // The facts the job records about its trigger: the pipeline, and the cap on rows per source when it was started as a test run (FDW-1730).
    // A run started without a cap records no cap, so a real run is told from a test by the cap's absence and nothing is invented for it.
    private static Dictionary<string, object?> TriggerFacts(string pipelineName, int? testRowsPerSource, string? webhook)
    {
        var facts = new Dictionary<string, object?> { ["PipelineName"] = pipelineName };
        if (testRowsPerSource is { } cap)
        {
            facts["TestRowsPerSource"] = cap;
        }

        // A run a webhook started names the webhook (FDW-1884); a run nobody called a webhook for records none.
        if (webhook is not null)
        {
            facts["WebhookName"] = webhook;
        }

        return facts;
    }

    // The pipeline, resolved by the id or the name it was named by and built in the view the trigger is entitled
    // to: its caller's own, or the relayed tenant's for a service caller with none. A schedule relays the id
    // (FDW-1088), so it resolves exactly the pipeline it stores, never another tenant's of the same name.
    private async Task<Resolution> ResolvePipeline(
        string? pipelineName, Guid? pipelineId, Guid? relayedTenant, CancellationToken ct)
    {
        using var view = new RelayedTenantView(_accessor, relayedTenant);
        var configuration = pipelineId is { } id
            ? await _pipelineConfigurations.Get(id, Fdw.Services.Abstractions.ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false)
            : await _pipelineConfigurations.Get(pipelineName!, Fdw.Services.Abstractions.ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (!configuration.IsSuccess || configuration.Value is null)
            return new Resolution(null, null, configuration.CurrentMessage, null, null);

        // Why whole and a graph: what a run reaches is what its components and stages use (FDW-1891), which only a pipeline read whole
        // holds; the graph is the one pipeline implementation, so any other has no reading of its reach and is refused, not guessed at.
        if (configuration.Value is not GraphPipelineConfiguration graph)
            return new Resolution(null, null, $"pipeline '{configuration.Value.Name}' is not a graph pipeline, so what it reaches cannot be read", null, null);

        var pipeline = await _pipelineProvider.Get(configuration.Value.Id, ct).ConfigureAwait(false);
        if (!pipeline.IsSuccess || pipeline.Value is null)
            return new Resolution(null, null, pipeline.CurrentMessage, null, null);
        pipeline.Value.Dispose();
        var declared = (configuration.Value as IDeclaresRunParameters)?.DeclaredRunParameters;
        return new Resolution(configuration.Value.Id, configuration.Value.Name, null, declared, graph);
    }

    // What resolving a pipeline found: its id, name, the parameters it declares and the graph read whole; or why it could not be found.
    private readonly record struct Resolution(
        Guid? Id, string? Name, string? NotFoundReason, IReadOnlyList<IPipelineRunParameter>? Declared, GraphPipelineConfiguration? Graph);

    // The roles judged for what the pipeline reaches, in the view the trigger is entitled to: the caller's own, or the relayed
    // tenant's for a service caller with none, so what is visible to the run is what would be visible to its tenant.
    private async Task<IGenericResult> AuthorizeRun(
        UnifiedTriggerRequest req, ClaimsPrincipal caller, PipelineReach reach, bool relayed, Guid? relayedTenant, CancellationToken ct)
    {
        using var view = new RelayedTenantView(_accessor, relayedTenant);
        return relayed
            ? await _runAuthority.AuthorizeScheduled(reach, RunRoleSelection.Chosen(req.RunRoles), _logger, ct).ConfigureAwait(false)
            : await _runAuthority.Authorize(
                new ClaimsPrincipalAuthenticationContext(caller), reach, RunRoleSelection.Chosen(req.RunRoles), _logger, ct).ConfigureAwait(false);
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
