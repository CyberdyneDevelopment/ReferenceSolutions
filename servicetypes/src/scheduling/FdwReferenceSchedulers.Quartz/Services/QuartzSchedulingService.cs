using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Execution.Abstractions.OptionTypes;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Execution;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;
using TriggerKinds = Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceSchedulers.Quartz.Logging;
using FdwReferenceSchedulers.Quartz.Translators;

namespace FdwReferenceSchedulers.Quartz.Services;

/// <summary>
/// Quartz-backed implementation of <see cref="IFrameworkSchedulingService"/>. Each write operation
/// builds the shared <see cref="ISchedulingCommand"/> for that operation, translates it through
/// <see cref="QuartzSchedulingTranslators"/>, then executes the resulting native call against the
/// live Quartz <see cref="IScheduler"/>. Execution history reads/writes go through
/// <see cref="ISchedulerImplementationConfiguration.DataStoreName"/>/<c>PathName</c>/
/// <c>ScheduleContainerName + "History"</c> — the target this scheduler's own configuration assigns,
/// the same values <see cref="Services.ScheduledPipelineJob"/> writes to per fire.
/// </summary>
public sealed class QuartzSchedulingService : IFrameworkSchedulingService
{
    private readonly ILogger<QuartzSchedulingService> _logger;
    private readonly IScheduler _scheduler;
    private readonly IDataGateway _dataGateway;
    private readonly ISchedulerImplementationConfiguration _configuration;

    /// <summary>Initializes a new instance of the <see cref="QuartzSchedulingService"/> class.</summary>
    public QuartzSchedulingService(
        ILogger<QuartzSchedulingService> logger,
        IScheduler scheduler,
        IDataGateway dataGateway,
        ISchedulerImplementationConfiguration configuration)
    {
        _logger = logger ?? NullLogger<QuartzSchedulingService>.Instance;
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _dataGateway = dataGateway ?? throw new ArgumentNullException(nameof(dataGateway));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    public IGenericScheduler Scheduler =>
        throw new NotSupportedException(
            "Direct scheduler access is not available in QuartzSchedulingService. Use the service methods for schedule management.");

    /// <inheritdoc />
    public string Id => _configuration.Id.ToString("N");

    /// <inheritdoc/>
    public string ServiceType => "Scheduler";

    /// <inheritdoc />
    public string Name => _configuration.Name;

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<T>.Failure(
            QuartzSchedulingLog.NativeCallFailed(_logger,
                new NotSupportedException("IGenericCommand execution is not supported; use the typed service methods."),
                "(unknown)")));

    /// <inheritdoc />
    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult((IGenericResult)GenericResult.Failure(
            QuartzSchedulingLog.NativeCallFailed(_logger,
                new NotSupportedException("IGenericCommand execution is not supported; use the typed service methods."),
                "(unknown)")));

    /// <inheritdoc />
    public async Task<IGenericResult> CreateSchedule(IGenericSchedule schedule, CancellationToken cancellationToken = default)
        => await Execute(
            new CreateScheduleCommand(schedule, _configuration.DataStoreName, _configuration.PathName, _configuration.ScheduleContainerName),
            QuartzSchedulingTranslators.Create, schedule.ScheduleId, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IGenericResult> UpdateSchedule(IGenericSchedule schedule, CancellationToken cancellationToken = default)
        => await Execute(
            new UpdateScheduleCommand(schedule, _configuration.DataStoreName, _configuration.PathName, _configuration.ScheduleContainerName),
            QuartzSchedulingTranslators.Update, schedule.ScheduleId, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IGenericResult> DeleteSchedule(string scheduleId, CancellationToken cancellationToken = default)
        => await Execute(
            new DeleteScheduleCommand(scheduleId, _configuration.DataStoreName, _configuration.PathName, _configuration.ScheduleContainerName),
            QuartzSchedulingTranslators.Delete, scheduleId, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IGenericResult> PauseSchedule(string scheduleId, CancellationToken cancellationToken = default)
        => await Execute(
            new PauseScheduleCommand(scheduleId, _configuration.DataStoreName, _configuration.PathName, _configuration.ScheduleContainerName),
            QuartzSchedulingTranslators.Pause, scheduleId, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IGenericResult> ResumeSchedule(string scheduleId, CancellationToken cancellationToken = default)
        => await Execute(
            new ResumeScheduleCommand(scheduleId, _configuration.DataStoreName, _configuration.PathName, _configuration.ScheduleContainerName),
            QuartzSchedulingTranslators.Resume, scheduleId, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IGenericResult> TriggerSchedule(string scheduleId, CancellationToken cancellationToken = default)
        => await Execute(
            new TriggerScheduleCommand(scheduleId, _configuration.DataStoreName, _configuration.PathName, _configuration.ScheduleContainerName),
            QuartzSchedulingTranslators.Trigger, scheduleId, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IGenericResult<IGenericSchedule?>> GetSchedule(string scheduleId, CancellationToken cancellationToken = default)
    {
        var triggerKey = new TriggerKey(scheduleId, "Schedules");
        var trigger = await _scheduler.GetTrigger(triggerKey, cancellationToken).ConfigureAwait(false);
        if (trigger is null)
            return GenericResult<IGenericSchedule?>.Success(null);

        var live = await ReadLive(scheduleId, trigger, cancellationToken).ConfigureAwait(false);
        return live.IsFailure
            ? live.ToNewResult<IGenericSchedule?>()
            : GenericResult<IGenericSchedule?>.Success(live.Value);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyCollection<IGenericSchedule>>> GetSchedules(
        bool includeInactive = true, CancellationToken cancellationToken = default)
    {
        var triggerKeys = await _scheduler.GetTriggerKeys(
            GroupMatcher<TriggerKey>.GroupEquals("Schedules"), cancellationToken).ConfigureAwait(false);

        var schedules = new List<IGenericSchedule>();
        foreach (var key in triggerKeys)
        {
            var trigger = await _scheduler.GetTrigger(key, cancellationToken).ConfigureAwait(false);
            if (trigger is null)
                continue;

            var state = await _scheduler.GetTriggerState(key, cancellationToken).ConfigureAwait(false);
            if (!includeInactive && state == TriggerState.Paused)
                continue;

            var live = await ReadLive(key.Name, trigger, cancellationToken).ConfigureAwait(false);
            if (live.IsFailure)
                return live.ToNewResult<IReadOnlyCollection<IGenericSchedule>>();

            schedules.Add(live.Value!);
        }

        return GenericResult<IReadOnlyCollection<IGenericSchedule>>.Success(schedules);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyCollection<IGenericScheduleExecutionHistory>>> GetScheduleHistory(
        string scheduleId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = Query.From<ScheduleExecutionRecord>(
                _configuration.DataStoreName,
                _configuration.PathName,
                _configuration.ScheduleContainerName + "History")
            .Where("ScheduleName", scheduleId);

        if (startDate.HasValue)
        {
            query = query.Where(r => r.TriggeredAt).GreaterThanOrEqual(new DateTimeOffset(startDate.Value, TimeSpan.Zero));
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.TriggeredAt).LessThanOrEqual(new DateTimeOffset(endDate.Value, TimeSpan.Zero));
        }

        var command = query.OrderBy("TriggeredAt").Build();
        var result = await _dataGateway.Execute<IEnumerable<ScheduleExecutionRecord>>(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
            return result.ToNewResult<IReadOnlyCollection<IGenericScheduleExecutionHistory>>();

        IReadOnlyCollection<IGenericScheduleExecutionHistory> history = result.Value!
            .Select(ToExecutionHistory)
            .ToList();

        return GenericResult<IReadOnlyCollection<IGenericScheduleExecutionHistory>>.Success(history);
    }

    private async Task<IGenericResult> Execute(
        ISchedulingCommand command, ISchedulingCommandTranslator<QuartzNativeAction> translator, string scheduleId, CancellationToken cancellationToken)
    {
        QuartzSchedulingLog.TranslatingCommand(_logger, command.CommandType, scheduleId);

        var translation = await translator.Translate(command, cancellationToken).ConfigureAwait(false);
        if (!translation.IsSuccess || translation.Value is null)
            return translation;

        return await translation.Value(_scheduler, cancellationToken).ConfigureAwait(false);
    }

    private static QuartzExecutionHistoryRecord ToExecutionHistory(ScheduleExecutionRecord record)
    {
        var status = ProcessStates.ByName(record.Status);
        return new QuartzExecutionHistoryRecord(
            executionId: record.Id.ToString("N"),
            scheduleId: record.ScheduleName,
            triggeredAt: record.TriggeredAt.UtcDateTime,
            startedAt: record.StartedAt?.UtcDateTime,
            completedAt: record.CompletedAt?.UtcDateTime,
            duration: record.DurationMs.HasValue ? TimeSpan.FromMilliseconds(record.DurationMs.Value) : null,
            wasTriggeredManually: string.Equals(record.TriggerType, "Manual", StringComparison.Ordinal),
            status: status,
            errorMessage: record.ErrorMessage);
    }

    // Reads back what Quartz actually holds for a schedule, so reconciliation compares like with like.
    // Why the job detail for the process id (FDW-902): the create translator writes it to the job's
    // data, not the trigger's; reading the trigger's map always came back empty.
    private async Task<IGenericResult<QuartzScheduleRecord>> ReadLive(string scheduleId, ITrigger trigger, CancellationToken cancellationToken)
    {
        var jobDetail = await _scheduler.GetJobDetail(trigger.JobKey, cancellationToken).ConfigureAwait(false);
        if (jobDetail?.JobDataMap.GetString(ScheduledPipelineJob.ProcessIdKey) is not { Length: > 0 } processId)
        {
            return GenericResult<QuartzScheduleRecord>.Failure(
                QuartzSchedulingLog.JobDataMissing(_logger, trigger.JobKey.ToString(), ScheduledPipelineJob.ProcessIdKey));
        }

        // Why the state too: a paused trigger keeps its next fire time, so "has a next fire time"
        // reported a paused schedule as active and reconciliation never resumed it on re-enable.
        var state = await _scheduler.GetTriggerState(trigger.Key, cancellationToken).ConfigureAwait(false);
        var isActive = state != TriggerState.Paused && trigger.GetNextFireTimeUtc().HasValue;
        var nextExecution = isActive ? trigger.GetNextFireTimeUtc()?.UtcDateTime : null;

        switch (trigger)
        {
            case ICronTrigger cron:
                return GenericResult<QuartzScheduleRecord>.Success(new QuartzScheduleRecord(
                    scheduleId, scheduleId, processId, nameof(TriggerKinds.Cron), cron.CronExpressionString,
                    intervalSeconds: null, executeAtUtc: null, isActive, cron.TimeZone.Id, nextExecution));

            case ISimpleTrigger simple when simple.RepeatCount == SimpleTriggerImpl.RepeatIndefinitely:
                // Why UTC: a simple trigger has no time zone; it fires on an interval from its start.
                return GenericResult<QuartzScheduleRecord>.Success(new QuartzScheduleRecord(
                    scheduleId, scheduleId, processId, nameof(TriggerKinds.Interval), cronExpression: null,
                    (int)simple.RepeatInterval.TotalSeconds, executeAtUtc: null, isActive, TimeZoneInfo.Utc.Id, nextExecution));

            case ISimpleTrigger simple when simple.RepeatCount == 0:
                return GenericResult<QuartzScheduleRecord>.Success(new QuartzScheduleRecord(
                    scheduleId, scheduleId, processId, nameof(TriggerKinds.Once), cronExpression: null,
                    intervalSeconds: null, simple.StartTimeUtc.UtcDateTime, isActive, TimeZoneInfo.Utc.Id, nextExecution));

            default:
                return GenericResult<QuartzScheduleRecord>.Failure(
                    QuartzSchedulingLog.UnrecognisedTrigger(_logger, scheduleId, trigger.GetType().Name));
        }
    }

    private sealed class QuartzScheduleRecord : IGenericSchedule
    {
        public QuartzScheduleRecord(
            string scheduleId, string scheduleName, string processId, string triggerType, string? cronExpression,
            int? intervalSeconds, DateTime? executeAtUtc, bool isActive, string timeZoneId, DateTime? nextExecution)
        {
            ScheduleId = scheduleId;
            ScheduleName = scheduleName;
            ProcessId = processId;
            TriggerType = triggerType;
            CronExpression = cronExpression;
            IntervalSeconds = intervalSeconds;
            ExecuteAtUtc = executeAtUtc;
            IsActive = isActive;
            TimeZoneId = timeZoneId;
            NextExecution = nextExecution;
        }

        public string ScheduleId { get; }
        public string ScheduleName { get; }
        public string ProcessId { get; }
        public string TriggerType { get; }
        public string? CronExpression { get; }
        public int? IntervalSeconds { get; }
        public DateTime? ExecuteAtUtc { get; }
        public DateTime? NextExecution { get; }
        public bool IsActive { get; }
        public string TimeZoneId { get; }
        public IReadOnlyDictionary<string, object>? Metadata => null;
    }

    private sealed class QuartzExecutionHistoryRecord : IGenericScheduleExecutionHistory
    {
        public QuartzExecutionHistoryRecord(
            string executionId, string scheduleId, DateTime triggeredAt, DateTime? startedAt,
            DateTime? completedAt, TimeSpan? duration, bool wasTriggeredManually,
            IProcessState status, string? errorMessage)
        {
            ExecutionId = executionId;
            ScheduleId = scheduleId;
            TriggeredAt = triggeredAt;
            StartedAt = startedAt;
            CompletedAt = completedAt;
            Duration = duration;
            WasTriggeredManually = wasTriggeredManually;
            Status = status;
            ErrorMessage = errorMessage;
        }

        public string ExecutionId { get; }
        public string ScheduleId { get; }
        public DateTime TriggeredAt { get; }
        public DateTime? StartedAt { get; }
        public DateTime? CompletedAt { get; }
        public TimeSpan? Duration { get; }
        public bool WasTriggeredManually { get; }
        public IProcessState Status { get; }
        public string? ErrorMessage { get; }
        public IReadOnlyDictionary<string, object>? Metadata => null;
    }
}
