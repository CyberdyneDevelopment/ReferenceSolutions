using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.JobTriggerSources;
using Fdw.Services.Execution.Abstractions;
using Fdw.Services.Execution.Abstractions.Models;
using Fdw.Services.Execution.Abstractions.OptionTypes.States;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using FdwReferenceSchedulers.EtlDispatch.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceSchedulers.EtlDispatch;

/// <summary>
/// Runs a firing schedule's pipeline by triggering it on the ETL server (FDW-902).
/// </summary>
/// <remarks>
/// The scheduler (Quartz) decides WHEN; this decides HOW: the process id is the pipeline
/// name, and the run is triggered through <see cref="IPipelineJobClient"/>, whose base address is the
/// <c>PipelineJobClient</c> HTTP connection. The schedule's own tenant is relayed, because a scheduled
/// fire has no caller whose token carries one, and so are the roles its creator recorded, because a fire has no caller to name them.
/// </remarks>
public sealed class EtlDispatchExecutionHandler : IGenericScheduledExecutionHandler
{
    private const string OperationName = "TriggerPipeline";

    private readonly IPipelineJobClient _client;
    private readonly IScheduleConfigurationProvider _schedules;
    private readonly ILogger<EtlDispatchExecutionHandler> _logger;

    /// <summary>Initializes a new instance of the <see cref="EtlDispatchExecutionHandler"/> class.</summary>
    /// <param name="client">The ETL server's pipeline job client.</param>
    /// <param name="schedules">Reads the firing schedule, for its tenant.</param>
    /// <param name="logger">The logger.</param>
    public EtlDispatchExecutionHandler(
        IPipelineJobClient client,
        IScheduleConfigurationProvider schedules,
        ILogger<EtlDispatchExecutionHandler>? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _schedules = schedules ?? throw new ArgumentNullException(nameof(schedules));
        _logger = logger ?? NullLogger<EtlDispatchExecutionHandler>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IProcessResult>> ExecuteScheduledProcess(
        string processId,
        string scheduleId,
        DateTime scheduledTime,
        DateTime actualTime,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(processId))
            return GenericResult<IProcessResult>.Failure(EtlDispatchLog.ProcessIdMissing(_logger, scheduleId));

        if (string.IsNullOrWhiteSpace(scheduleId))
            return GenericResult<IProcessResult>.Failure(EtlDispatchLog.ScheduleIdMissing(_logger, processId));

        EtlDispatchLog.Dispatching(_logger, processId, scheduleId, scheduledTime, actualTime);

        var schedule = await _schedules.Get(scheduleId, cancellationToken).ConfigureAwait(false);
        if (schedule.IsFailure || schedule.Value is null)
        {
            return GenericResult<IProcessResult>.Failure(
                EtlDispatchLog.ScheduleUnreadable(_logger, scheduleId, processId, schedule.CurrentMessage));
        }

        // The process id is the pipeline's id (FDW-1088). The schedule row read just now is the authority: a job
        // still holding another pipeline is stale and is refused, never run.
        if (!Guid.TryParse(processId, out var pipelineId))
            return GenericResult<IProcessResult>.Failure(EtlDispatchLog.ProcessIdNotPipelineId(_logger, processId, scheduleId));
        if (pipelineId != schedule.Value.PipelineId)
            return GenericResult<IProcessResult>.Failure(
                EtlDispatchLog.ProcessIdStale(_logger, processId, scheduleId, schedule.Value.PipelineId));

        // The key of this fire (FDW-4317): the schedule's Id and the time the fire was due, the same on every retry of it, so the ETL host answers a retry (after a 504, for example)
        // with the run the first call started. The due time, not the time of this attempt, is what names the fire.
        var fireTime = new DateTimeOffset(scheduledTime.ToUniversalTime());
        var triggered = await _client.Trigger(
            new TriggerPipelineRequest
            {
                Id = pipelineId,
                TriggerSource = JobTriggerSourceTypes.Scheduled.Name,
                TriggerKey = string.Concat(schedule.Value.Id.ToString("D", System.Globalization.CultureInfo.InvariantCulture), ":", fireTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture)),
                TriggerKeyFireTime = fireTime,
                ScheduleName = scheduleId,

                // The schedule's domain Id (FDW-3975): the thresholds attached to the schedule are found by it, and the schedule was read just now.
                ScheduleId = schedule.Value.Id,

                // The timeout the schedule states (FDW-3805): the ETL host stops the run when it has passed. Null is a schedule that states none, and its run has no time limit.
                TimeoutSeconds = schedule.Value.TimeoutSeconds,
                TenantId = schedule.Value.TenantId,
                Parameters = [.. schedule.Value.Parameters.Select(p => new PipelineParameterValue { Name = p.Key, Value = string.Concat(p.Value) })],
                // The roles the schedule's creator named (FDW-1161): every fire acts with exactly these, judged again for what the
                // pipeline reaches when it fires.
                RunRoles = [.. schedule.Value.RunRoleNames],
            },
            cancellationToken).ConfigureAwait(false);

        if (triggered.IsFailure || triggered.Value is null)
        {
            // Why the upstream failure is kept, not wrapped (FDW-4293, as FDW-4211 did for connections): the failure's own code says whether a retry could
            // mend it (a 503 or 504 can, a 400 or 403 cannot), and a new failure here carried one retryable code for every cause, so a refusal would have been
            // retried. The line below says which pipeline and schedule it was; the result is the ETL client's, code chain and messages as they came.
            EtlDispatchLog.TriggerFailed(_logger, processId, scheduleId, triggered.CurrentMessage);
            return triggered.ToNewResult<IProcessResult>();
        }

        EtlDispatchLog.Queued(_logger, processId, scheduleId, triggered.Value.ExecutionId);

        // Why Pending: the ETL server queues the run and answers at once; the run itself completes
        // there, and its outcome is recorded against the execution id, not here.
        // Why the option itself and not ProcessStates.Pending: every ProcessStates option is
        // RestrictToCurrentCompilation, so the collection is empty outside its own assembly (board
        // item processstates-empty-and-mismatched).
        return GenericResult<IProcessResult>.Success(
            ProcessResult.Success(processId, OperationName, new Pending(), triggered.Value));
    }

    /// <inheritdoc />
    public Task<IGenericResult<bool>> CanHandleProcess(string processId, CancellationToken cancellationToken = default)
    {
        // Why only presence: whether the pipeline exists is the ETL server's answer at trigger time -- this handler
        // holds no pipeline catalogue to check against; that the id parses is checked when it fires.
        var canHandle = !string.IsNullOrWhiteSpace(processId);
        EtlDispatchLog.CanHandle(_logger, processId, canHandle);
        return Task.FromResult<IGenericResult<bool>>(GenericResult<bool>.Success(canHandle));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IProcess?>> GetProcessInfo(string processId, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult<IProcess?>>(
            GenericResult<IProcess?>.Failure(EtlDispatchLog.ProcessInfoNotHeld(_logger, processId)));
}
