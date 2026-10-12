using System;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Execution.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
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
    private readonly ILogger<ScheduledPipelineJob> _logger;

    private IDataGateway Gateway => _dataGateways.ByName(MainDataGatewayName);

    /// <summary>Initializes a new instance of the <see cref="ScheduledPipelineJob"/> class.</summary>
    public ScheduledPipelineJob(
        IGenericScheduledExecutionHandler executionHandler,
        IDataGatewayProvider dataGateways,
        IAuthenticationContextAccessor authenticationContextAccessor,
        ILogger<ScheduledPipelineJob>? logger = null)
    {
        _executionHandler = executionHandler ?? throw new ArgumentNullException(nameof(executionHandler));
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
        _authenticationContextAccessor = authenticationContextAccessor ?? throw new ArgumentNullException(nameof(authenticationContextAccessor));
        _logger = logger ?? NullLogger<ScheduledPipelineJob>.Instance;
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
            scheduledFireTime, actualFireTime, status, errorMessage, context.CancellationToken)
            .ConfigureAwait(false);

        await RecordRunFields(
            scheduleId, dataStoreName, pathName, scheduleContainerName,
            actualFireTime, context.NextFireTimeUtc, status, context.CancellationToken)
            .ConfigureAwait(false);
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
    private async Task RecordRunFields(
        string scheduleId, string dataStoreName, string pathName, string scheduleContainerName,
        DateTime actualFireTime, DateTimeOffset? nextFireTime, string status,
        System.Threading.CancellationToken cancellationToken)
    {
        var command = Update.In<ScheduleUpdateRecord>(scheduleContainerName)
            .DataStore(dataStoreName)
            .Path(pathName)
            .Where("Name", scheduleId)
            .Value(new ScheduleUpdateRecord
            {
                LastRunTime = new DateTimeOffset(DateTime.SpecifyKind(actualFireTime, DateTimeKind.Utc)),
                NextRunTime = nextFireTime,
                LastRunStatus = status,
            });

        var updated = await Gateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            QuartzSchedulingLog.RunFieldsWriteFailed(_logger, scheduleId, updated.CurrentMessage);
            return;
        }

        QuartzSchedulingLog.RunFieldsWritten(_logger, scheduleId, status, nextFireTime);
    }

    private async Task RecordExecution(
        string scheduleId, string processId, string dataStoreName, string pathName, string scheduleContainerName,
        DateTime scheduledFireTime, DateTime actualFireTime,
        string status, string? errorMessage, System.Threading.CancellationToken cancellationToken)
    {
        var completedAt = DateTimeOffset.UtcNow;
        var record = new ScheduleExecutionRecord
        {
            Name = $"{scheduleId}_{completedAt:yyyyMMddHHmmss}",
            ScheduleName = scheduleId,
            PipelineName = processId,
            TriggerType = "Scheduled",
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
