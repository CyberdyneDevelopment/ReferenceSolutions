using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Execution.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Data;
using Fdw.Services.Scheduling.Execution;
using Quartz;
using Microsoft.Extensions.Logging;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceSchedulers.Quartz.Logging;
using ProcessStateKinds = Fdw.Services.Execution.Abstractions.OptionTypes.States;

namespace FdwReferenceSchedulers.Quartz.Services;

/// <summary>
/// Bridges Quartz's execution callback to <see cref="IGenericScheduledExecutionHandler"/> — Quartz
/// resolves this class per fire via its DI job factory (constructor injection), then calls
/// <see cref="Execute"/> for every trigger that names it. Also records the fire in the scheduler's
/// own execution-history container.
/// </summary>
/// <remarks>
/// The target (DataStoreName/PathName/ScheduleContainerName) comes off the job's own
/// <see cref="IJobExecutionContext.JobDetail"/> data, put there by
/// <see cref="Translators.QuartzCreateScheduleTranslator"/> from the command it translated —
/// not injected here, because there is no single <see cref="ISchedulerImplementationConfiguration"/>
/// DI can hand a job resolved dynamically per fire; the command that scheduled this job already
/// carried it.
/// </remarks>
public sealed class ScheduledPipelineJob : IJob
{
    private const string HistoryContainerSuffix = "History";

    /// <summary>The job data key naming the process to execute.</summary>
    public const string ProcessIdKey = "ProcessId";

    /// <summary>The job data key naming the schedule that owns this job.</summary>
    public const string ScheduleIdKey = "ScheduleId";

    /// <summary>The job data key carrying the time a catch-up run was due (FDW-3788), written round-trip; absent from every other fire.</summary>
    public const string CatchupDueKey = "CatchupDue";

    /// <summary>The trigger type an ordinary fire is recorded with.</summary>
    private const string ScheduledTriggerType = "Scheduled";

    /// <summary>The trigger type a run for the fire times missed while the scheduler was down is recorded with (FDW-3788).</summary>
    private const string CatchupTriggerType = "Catchup";

    /// <summary>The job data key carrying the number of this attempt on a retry trigger (FDW-4293); absent from a fire that is not a retry, which is attempt 0.</summary>
    public const string RetryAttemptKey = "RetryAttempt";

    /// <summary>The job data key carrying the time the fire being retried was scheduled for (FDW-4293), written round-trip; present exactly when <see cref="RetryAttemptKey"/> is.</summary>
    public const string RetriedFireKey = "RetriedFire";

    /// <summary>The trigger group a retry's one-shot trigger lives in, apart from the schedule's own trigger group so listing and reconciling schedules never see it (FDW-4293).</summary>
    public const string RetryTriggerGroup = "Retries";

    /// <summary>The trigger type a retry of a failed dispatch is recorded with (FDW-4293).</summary>
    private const string RetryTriggerType = "Retry";

    /// <summary>The job data key naming the connection to record execution history in.</summary>
    public const string DataStoreNameKey = "DataStoreName";

    /// <summary>The job data key naming the schema to record execution history in.</summary>
    public const string PathNameKey = "PathName";

    /// <summary>The job data key naming the schedule container, whose history container this appends to.</summary>
    public const string ScheduleContainerNameKey = "ScheduleContainerName";

    // Why the provider and not IDataGateway: FDW never registers IDataGateway. The data-gateway option
    // registers IDataGatewayFactory and IDataGatewayProvider only, because Create() builds a fresh gateway
    // per ask and a registration would capture one across a scope boundary. A job that asks DI for the
    // gateway fails when it is built -- "Unable to resolve service for type IDataGateway" -- and Quartz
    // then sets every trigger of the job to ERROR, so the schedule never fires (FDW-1055). Services take
    // providers; only providers take gateways.
    private const string MainDataGatewayName = "Main";

    private readonly IGenericScheduledExecutionHandler _executionHandler;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IAuthenticationContextAccessor _authenticationContextAccessor;
    private readonly IScheduleConfigurationProvider _schedules;
    private readonly IScheduleRunRecorder _runRecorder;
    private readonly ILogger<ScheduledPipelineJob> _logger;

    private IDataGateway Gateway => _dataGateways.ByName(MainDataGatewayName);

    /// <summary>Initializes a new instance of the <see cref="ScheduledPipelineJob"/> class.</summary>
    public ScheduledPipelineJob(
        IGenericScheduledExecutionHandler executionHandler,
        IDataGatewayProvider dataGateways,
        IAuthenticationContextAccessor authenticationContextAccessor,
        IScheduleConfigurationProvider schedules,
        IScheduleRunRecorder runRecorder,
        ILogger<ScheduledPipelineJob>? logger = null)
    {
        _executionHandler = executionHandler ?? throw new ArgumentNullException(nameof(executionHandler));
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
        _authenticationContextAccessor = authenticationContextAccessor ?? throw new ArgumentNullException(nameof(authenticationContextAccessor));
        _schedules = schedules ?? throw new ArgumentNullException(nameof(schedules));
        _runRecorder = runRecorder ?? throw new ArgumentNullException(nameof(runRecorder));
        _logger = logger ?? NullLogger<ScheduledPipelineJob>.Instance;
    }

    /// <summary>
    /// Says which kind of fire a trigger's data describes (FDW-4387): an ordinary fire, a catch-up for a time that passed (FDW-3788) or a retry of a failed dispatch (FDW-4293).
    /// </summary>
    /// <remarks>
    /// Why each key is read only where it is present: an ordinary fire carries none of <see cref="CatchupDueKey"/>, <see cref="RetryAttemptKey"/> and <see cref="RetriedFireKey"/>, and
    /// Quartz's <c>GetString</c> throws <see cref="System.Collections.Generic.KeyNotFoundException"/> for an absent key, which failed every ordinary fire before its pipeline started.
    /// A key that is present and empty, not readable as what it holds, or (a retry) without its partner key, is unreadable: the answer names it and the fire records nothing.
    /// There is no default and no fallback.
    /// </remarks>
    /// <param name="data">The merged job data of the fire (the job's data and its trigger's).</param>
    /// <returns>The kind of fire, the time it was scheduled for when that is not the time Quartz says, its retry attempt (0 when it is not a retry), or the key that could not be read.</returns>
    public static ScheduledFireKind ReadFireKind(JobDataMap data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var kind = new ScheduledFireKind(ScheduledTriggerType, null, 0, null);
        if (data.ContainsKey(CatchupDueKey))
        {
            if (!TryReadTime(data.GetString(CatchupDueKey), out var due))
            {
                return kind with { UnreadableKey = CatchupDueKey };
            }

            kind = kind with { TriggerType = CatchupTriggerType, ScheduledFireTime = due };
        }

        if (!data.ContainsKey(RetryAttemptKey))
        {
            return kind;
        }

        // The retry replaces a catch-up: a retry of a fire is recorded against the time of that fire.
        return int.TryParse(data.GetString(RetryAttemptKey), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var attempt)
            && attempt >= 1
            && data.ContainsKey(RetriedFireKey)
            && TryReadTime(data.GetString(RetriedFireKey), out var retriedFire)
                ? kind with { TriggerType = RetryTriggerType, ScheduledFireTime = retriedFire, RetryAttempt = attempt }
                : kind with { UnreadableKey = RetryAttemptKey };
    }

    // A round-trip time as the translator writes it; empty or null text is not one.
    private static bool TryReadTime(string? text, out DateTime utc)
    {
        if (DateTimeOffset.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var time))
        {
            utc = time.UtcDateTime;
            return true;
        }

        utc = default;
        return false;
    }

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        // Why no defaults (FDW-902): the create translator writes every one of these keys, so a missing
        // one is a job registered some other way; running it against an empty name would trigger and
        // record nothing identifiable.
        var jobDataMap = context.JobDetail.JobDataMap;
        var jobKey = context.JobDetail.Key.ToString();
        if (jobDataMap.GetString(ProcessIdKey) is not { Length: > 0 } processId)
        {
            QuartzSchedulingLog.JobDataMissing(_logger, jobKey, ProcessIdKey);
            return;
        }

        if (jobDataMap.GetString(ScheduleIdKey) is not { Length: > 0 } scheduleId)
        {
            QuartzSchedulingLog.JobDataMissing(_logger, jobKey, ScheduleIdKey);
            return;
        }

        if (jobDataMap.GetString(DataStoreNameKey) is not { Length: > 0 } dataStoreName)
        {
            QuartzSchedulingLog.JobDataMissing(_logger, jobKey, DataStoreNameKey);
            return;
        }

        if (jobDataMap.GetString(PathNameKey) is not { Length: > 0 } pathName)
        {
            QuartzSchedulingLog.JobDataMissing(_logger, jobKey, PathNameKey);
            return;
        }

        if (jobDataMap.GetString(ScheduleContainerNameKey) is not { Length: > 0 } scheduleContainerName)
        {
            QuartzSchedulingLog.JobDataMissing(_logger, jobKey, ScheduleContainerNameKey);
            return;
        }

        // Why the fire time when there is no scheduled time: Quartz leaves ScheduledFireTimeUtc null only
        // for a manual fire (TriggerSchedule), which was scheduled for the moment it fired.
        var scheduledFireTime = context.ScheduledFireTimeUtc?.UtcDateTime ?? context.FireTimeUtc.UtcDateTime;
        var actualFireTime = context.FireTimeUtc.UtcDateTime;

        // Why the trigger's data says which kind of fire this is (FDW-3788, FDW-4293, FDW-4387): an ordinary fire carries none of the catch-up and retry keys, so they are read only
        // where they are present. A key that is present and cannot be read fails the fire loudly and records nothing, never a fire due now.
        var fire = ReadFireKind(context.MergedJobDataMap);
        if (fire.UnreadableKey is { } unreadableKey)
        {
            QuartzSchedulingLog.JobDataMissing(_logger, jobKey, unreadableKey);
            return;
        }

        var triggerType = fire.TriggerType;
        var retryAttempt = fire.RetryAttempt;
        if (fire.ScheduledFireTime is { } kindScheduledFireTime)
        {
            scheduledFireTime = kindScheduledFireTime;
        }

        // Why elevated: a background fire has no ClaimsPrincipal, so without this the connection
        // resolves to the deny-everywhere principal and reaches only fn_TenantFilter's shared-row
        // branch — reads come back partial and silent, and an insert whose row is not visible is
        // rejected outright by SchedPolicy's AFTER INSERT block predicate. System elevation is the
        // decision recorded in FDW-767: deliberate, temporary, and it means this run is not
        // tenant-scoped. Revisit by giving the run a real Guid principal.
        using var elevation = new SystemAuthenticationContextScope(_authenticationContextAccessor);

        var result = await _executionHandler.ExecuteScheduledProcess(
            processId,
            scheduleId,
            scheduledFireTime,
            actualFireTime,
            metadata: null,
            context.CancellationToken).ConfigureAwait(false);

        // Why the error is set per path (FDW-902): the row's error column holds why a run failed, and
        // nothing on a real success -- a success's CurrentMessage is not an error, and a success with no
        // process result is recorded as Failed, so its row names that reason.
        string? errorMessage = null;
        if (!result.IsSuccess)
        {
            errorMessage = result.CurrentMessage;
            QuartzSchedulingLog.JobExecutionFailed(_logger, scheduleId, processId, result.CurrentMessage);
        }
        else if (result.Value is null)
        {
            errorMessage = QuartzSchedulingLog.HandlerReturnedNoResult(_logger, scheduleId, processId).Message;
        }

        var status = StatusOf(result);

        await RecordExecution(
            scheduleId, processId, dataStoreName, pathName, scheduleContainerName,
            triggerType, retryAttempt, scheduledFireTime, actualFireTime, status, errorMessage, context.CancellationToken)
            .ConfigureAwait(false);

        // Why a retry asks the schedule's own triggers for the next run (FDW-4293): a retry's trigger fires once and has no next fire time of its own,
        // and writing that over the schedule's NextRunTime would say the schedule never runs again.
        var nextFireTime = retryAttempt > 0
            ? await NextScheduledFire(context).ConfigureAwait(false)
            : context.NextFireTimeUtc;

        await RecordRunFields(scheduleId, actualFireTime, nextFireTime, status, context.CancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await ScheduleRetry(context, scheduleId, retryAttempt, scheduledFireTime, result, context.CancellationToken).ConfigureAwait(false);
        }
    }

    // Why only a failure whose code says it can be retried (FDW-4293): the dispatch failed because the ETL did not accept the trigger, and a refusal that
    // would be refused again (a 400, a 403, a schedule that no longer names the pipeline) is not made better by waiting. A run the ETL accepted and
    // later fails is the ETL's to recover (FDW-3783); the scheduler never sees it, because the handler answers Pending the moment the run is queued.
    // Why a one-shot trigger of its own and not a wait here: a wait would hold one of the engine's few threads for the whole delay, and Quartz's
    // own refire has no delay. The retry trigger lives in its own group because it is not the schedule's trigger, and the schedule's own listing and reconciliation look only at that.
    // Why the schedule is read now and not carried on the job: an edit to MaxRetries or RetryDelaySeconds applies to the next failure, as it does to the next fire.
    private async Task ScheduleRetry(
        IJobExecutionContext context, string scheduleId, int attempt, DateTime scheduledFireTime,
        IGenericResult<IProcessResult> failure, System.Threading.CancellationToken cancellationToken)
    {
        if (failure.Code is not { IsRetryable: true })
        {
            QuartzSchedulingLog.RetryNotApplicable(_logger, scheduleId, attempt, failure.Code?.Name ?? string.Empty);
            return;
        }

        var schedule = await _schedules.Get(scheduleId, cancellationToken).ConfigureAwait(false);
        if (schedule.IsFailure || schedule.Value is null)
        {
            QuartzSchedulingLog.RetryNotScheduled(_logger, scheduleId, attempt + 1, schedule.CurrentMessage);
            return;
        }

        if (attempt >= schedule.Value.MaxRetries)
        {
            QuartzSchedulingLog.RetriesExhausted(_logger, scheduleId, attempt, schedule.Value.MaxRetries);
            return;
        }

        if (schedule.Value.RetryDelaySeconds < 0)
        {
            QuartzSchedulingLog.RetryNotScheduled(_logger, scheduleId, attempt + 1, $"its RetryDelaySeconds is {schedule.Value.RetryDelaySeconds}, which is below zero");
            return;
        }

        var next = attempt + 1;
        var due = DateTimeOffset.UtcNow.AddSeconds(schedule.Value.RetryDelaySeconds);
        var data = new JobDataMap
        {
            [RetryAttemptKey] = next.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [RetriedFireKey] = new DateTimeOffset(DateTime.SpecifyKind(scheduledFireTime, DateTimeKind.Utc)).ToString("o", System.Globalization.CultureInfo.InvariantCulture),
        };

        // Why the fire time in the name: one fire has at most one retry per attempt, and a name that repeats means two retries of one fire are being scheduled.
        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{scheduleId}_{scheduledFireTime:yyyyMMddHHmmss}_{next}", RetryTriggerGroup)
            .ForJob(context.JobDetail.Key)
            .StartAt(due)
            .UsingJobData(data)
            .Build();

        try
        {
            await context.Scheduler.ScheduleJob(trigger, cancellationToken).ConfigureAwait(false);
        }
        catch (SchedulerException ex)
        {
            QuartzSchedulingLog.RetryNotScheduled(_logger, scheduleId, next, ex.Message);
            return;
        }

        QuartzSchedulingLog.RetryScheduled(_logger, scheduleId, next, schedule.Value.MaxRetries, due);
    }

    private static async Task<DateTimeOffset?> NextScheduledFire(IJobExecutionContext context)
    {
        var triggers = await context.Scheduler.GetTriggersOfJob(context.JobDetail.Key, context.CancellationToken).ConfigureAwait(false);
        return triggers
            .Where(t => !string.Equals(t.Key.Group, RetryTriggerGroup, StringComparison.Ordinal))
            .Select(t => t.GetNextFireTimeUtc())
            .Where(next => next.HasValue)
            .OrderBy(next => next)
            .FirstOrDefault();
    }

    // Why the handler's own state (FDW-902): a success means the handler did its part, not that the
    // process finished -- EtlDispatch returns Pending once the ETL server has queued the run. Recording
    // "Succeeded" there claimed an outcome nobody had seen, and was not a ProcessStates name, so reading
    // the history back through the collection resolved to nothing.
    private static string StatusOf(IGenericResult<IProcessResult> result)
        => result.IsSuccess && result.Value is { } processResult
            ? processResult.FinalState.Name
            : nameof(ProcessStateKinds.Failed);

    // Why (FDW-902): the schedule row's LastRunTime / NextRunTime / LastRunStatus are what a caller
    // reads to see when a schedule last ran and runs next. The host loop that used to write them was
    // deleted when Quartz took over, and nothing else did. NextRunTime is Quartz's own next fire
    // time -- the engine that fires the trigger is the one that knows it. A targeted update, not a
    // versioned save: these are run fields, not a configuration change.
    // Why through the recorder and not an update of the container named in the job data (FDW-4598): the three fields are columns of the schedule's implementation row, which the
    // domain container the job data names does not have, so that update found nothing to set on every fire. The recorder is the provider that owns the columns and takes the
    // schedule's durable Id, which this reads from the schedule provider by the name the job carries.
    private async Task RecordRunFields(
        string scheduleId, DateTime actualFireTime, DateTimeOffset? nextFireTime, string status, System.Threading.CancellationToken cancellationToken)
    {
        var schedule = await _schedules.Get(scheduleId, cancellationToken).ConfigureAwait(false);
        if (schedule.IsFailure || schedule.Value is not { } found)
        {
            QuartzSchedulingLog.RunFieldsWriteFailed(_logger, scheduleId, schedule.CurrentMessage);
            return;
        }

        var recorded = await _runRecorder.RecordRun(
            found.Id, new DateTimeOffset(DateTime.SpecifyKind(actualFireTime, DateTimeKind.Utc)), nextFireTime, status, cancellationToken).ConfigureAwait(false);
        if (recorded.IsFailure)
        {
            QuartzSchedulingLog.RunFieldsWriteFailed(_logger, scheduleId, recorded.CurrentMessage);
            return;
        }

        QuartzSchedulingLog.RunFieldsWritten(_logger, scheduleId, status, nextFireTime);
    }

    private async Task RecordExecution(
        string scheduleId, string processId, string dataStoreName, string pathName, string scheduleContainerName,
        string triggerType, int retryAttempt, DateTime scheduledFireTime, DateTime actualFireTime,
        string status, string? errorMessage, System.Threading.CancellationToken cancellationToken)
    {
        var completedAt = DateTimeOffset.UtcNow;
        var record = new ScheduleExecutionRecord
        {
            Name = $"{scheduleId}_{completedAt:yyyyMMddHHmmss}",
            ScheduleName = scheduleId,
            PipelineName = processId,
            TriggerType = triggerType,
            RetryAttempt = retryAttempt,
            Status = status,
            TriggeredAt = scheduledFireTime,
            ScheduledFireTime = scheduledFireTime,
            StartedAt = actualFireTime,
            CompletedAt = completedAt,
            DurationMs = (long)(completedAt - actualFireTime).TotalMilliseconds,
            ErrorMessage = errorMessage,
            ExecutedBy = Environment.MachineName
        };

        var command = Insert.Into<ScheduleExecutionRecord>(scheduleContainerName + HistoryContainerSuffix)
            .DataStore(dataStoreName)
            .Path(pathName)
            .Value(record);

        var insertResult = await Gateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);
        if (!insertResult.IsSuccess)
        {
            QuartzSchedulingLog.HistoryWriteFailed(_logger, scheduleId, insertResult.CurrentMessage);
        }
    }
}
