using System;
using Fdw.Services.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Microsoft.Extensions.Logging;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceSchedulers.Quartz.Logging;

namespace FdwReferenceSchedulers.Quartz.Services;

/// <summary>
/// Reconciles Quartz's live schedule set to <c>sched.Schedule</c>: configuration is the one source
/// of truth. Enabled and missing from the engine → create. Present but changed → update. Disabled
/// but still live → pause (Quartz keeps a paused trigger registered at no cost, so pausing rather
/// than deleting means a later re-enable does not need to rebuild the job).
/// </summary>
/// <remarks>
/// Runs on the inherited poll. The configuration providers raise no change notification, so a
/// config change takes effect on the next evaluation rather than immediately.
/// <see cref="_evaluationLock"/> keeps an overrunning evaluation from overlapping the next one
/// against the same live trigger set.
/// </remarks>
public sealed class QuartzReconciliationService : SchedulerBackgroundServiceBase
{
    private readonly ILogger<QuartzReconciliationService> _logger;
    private readonly IScheduleConfigurationProvider _scheduleProvider;
    private readonly IDomainConfigurationProvider<ISchedulerImplementationConfiguration> _quartzConfigProvider;
    private readonly IQuartzSchedulingFactory _factory;
    private readonly TimeSpan _interval;
    private readonly SemaphoreSlim _evaluationLock = new(1, 1);

    private int _lastEvaluatedCount;

    /// <summary>Initializes a new instance of the <see cref="QuartzReconciliationService"/> class.</summary>
    public QuartzReconciliationService(
        IScheduleConfigurationProvider scheduleProvider,
        IDomainConfigurationProvider<ISchedulerImplementationConfiguration> quartzConfigProvider,
        IQuartzSchedulingFactory factory,
        IAuthenticationContextAccessor authenticationContextAccessor,
        TimeSpan interval,
        ILogger<QuartzReconciliationService>? logger = null)
        : base(authenticationContextAccessor, logger)
    {
        _scheduleProvider = scheduleProvider ?? throw new ArgumentNullException(nameof(scheduleProvider));
        _quartzConfigProvider = quartzConfigProvider ?? throw new ArgumentNullException(nameof(quartzConfigProvider));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _interval = interval;
        _logger = logger ?? NullLogger<QuartzReconciliationService>.Instance;

        // Why no change subscription: the configuration providers raise no change notification, so
        // reconciliation is interval-driven. A schedule edit is picked up on the next evaluation
        // rather than immediately.
    }

    /// <inheritdoc />
    protected override TimeSpan EvaluationInterval => _interval;

    /// <inheritdoc />
    protected override int LastEvaluatedCount => _lastEvaluatedCount;

    /// <inheritdoc />
    protected override async Task Evaluate(CancellationToken cancellationToken)
    {
        if (!await _evaluationLock.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false))
        {
            // A reconciliation pass is already running (the poll and the Changed event landed at
            // the same time) — that pass will see whatever just changed, so skipping here is
            // correct, not a dropped update.
            return;
        }

        try
        {
            await EvaluateCore(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _evaluationLock.Release();
        }
    }

    private async Task EvaluateCore(CancellationToken cancellationToken)
    {
        // Runs in the system context: SchedulerBackgroundServiceBase elevates every pass (FDW-825),
        // so a tenant-scoped schedule is read and registered like any other.

        var configsResult = await _quartzConfigProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!configsResult.IsSuccess || configsResult.Value is null || configsResult.Value.Count == 0)
        {
            QuartzSchedulingLog.NativeCallFailed(_logger,
                new InvalidOperationException("No Quartz scheduler configuration row found; nothing to reconcile against."),
                "(unknown)");
            return;
        }

        // Why the first row: this reference implementation expects exactly one active Quartz
        // scheduler per host. Reconciling more than one would mean deciding which one owns a given
        // schedule, which sched.Schedule has no column for today.
        var configuration = (ISchedulerImplementationConfiguration)configsResult.Value[0];

        var serviceResult = _factory.Create(configuration);
        if (!serviceResult.IsSuccess || serviceResult.Value is null)
        {
            QuartzSchedulingLog.NativeCallFailed(_logger,
                new InvalidOperationException(serviceResult.CurrentMessage ?? "Failed to build the Quartz scheduling service."),
                "(unknown)");
            return;
        }

        var service = serviceResult.Value;

        var schedulesResult = await _scheduleProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!schedulesResult.IsSuccess || schedulesResult.Value is null)
        {
            QuartzSchedulingLog.NativeCallFailed(_logger,
                new InvalidOperationException(schedulesResult.CurrentMessage ?? "Failed to read sched.Schedule."),
                "(unknown)");
            return;
        }

        var liveResult = await service.GetSchedules(includeInactive: true, cancellationToken).ConfigureAwait(false);
        if (!liveResult.IsSuccess || liveResult.Value is null)
        {
            QuartzSchedulingLog.NativeCallFailed(_logger,
                new InvalidOperationException(liveResult.CurrentMessage ?? "Failed to read Quartz's live trigger set."),
                "(unknown)");
            return;
        }

        var configured = schedulesResult.Value;
        var live = liveResult.Value.ToDictionary(s => s.ScheduleId, StringComparer.Ordinal);
        var evaluated = 0;

        foreach (var schedule in configured)
        {
            evaluated++;
            var liveSchedule = live.GetValueOrDefault(schedule.Name);

            if (schedule.IsEnabled)
            {
                // Why skipped (FDW-902): Event and Manual schedules fire on an event or a person, and a
                // Window has no bounds on a schedule row; Quartz only fires on a clock.
                if (!QuartzTriggers.IsClockDriven(schedule.ScheduleType))
                {
                    QuartzSchedulingLog.ScheduleNotClockDriven(_logger, schedule.Name, schedule.ScheduleType);
                    if (liveSchedule is not null)
                        await service.PauseSchedule(schedule.Name, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                // Why: Quartz drops a Once trigger after it fires, so without this the next pass would
                // re-create it with a start time in the past and it would fire again every minute.
                if (IsSpentOnce(schedule))
                    continue;

                var desired = ToGenericSchedule(schedule);
                if (liveSchedule is null)
                {
                    await service.CreateSchedule(desired, cancellationToken).ConfigureAwait(false);
                }
                else if (!string.Equals(liveSchedule.ProcessId, desired.ProcessId, StringComparison.Ordinal))
                {
                    // Why replace, not reschedule (FDW-1088): the pipeline id is the job's data, and rescheduling
                    // changes only its trigger. A job left holding the old id would run the old pipeline.
                    await service.DeleteSchedule(schedule.Name, cancellationToken).ConfigureAwait(false);
                    await service.CreateSchedule(desired, cancellationToken).ConfigureAwait(false);
                }
                else if (!IsSameTrigger(liveSchedule, desired))
                {
                    await service.UpdateSchedule(desired, cancellationToken).ConfigureAwait(false);
                }
                else if (!liveSchedule.IsActive)
                {
                    // Why: disabling pauses the trigger rather than deleting it, so re-enabling finds
                    // the same trigger still registered and paused -- it has to be resumed.
                    await service.ResumeSchedule(schedule.Name, cancellationToken).ConfigureAwait(false);
                }
            }
            else if (liveSchedule is not null)
            {
                await service.PauseSchedule(schedule.Name, cancellationToken).ConfigureAwait(false);
            }
        }

        // Anything live that configuration no longer names at all (row deleted, not just disabled)
        // gets paused the same way — sched.Schedule, not the engine, decides what should run.
        var configuredNames = configured.Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var liveSchedule in live.Values)
        {
            if (!configuredNames.Contains(liveSchedule.ScheduleId))
            {
                await service.PauseSchedule(liveSchedule.ScheduleId, cancellationToken).ConfigureAwait(false);
            }
        }

        _lastEvaluatedCount = evaluated;
    }

    // A Once schedule whose recorded last run is at or after its execution time has already fired.
    private static bool IsSpentOnce(IScheduleImplementationConfiguration schedule)
        => string.Equals(schedule.ScheduleType, nameof(Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations.Once), StringComparison.Ordinal)
           && schedule.LastRunTime is { } lastRun
           && schedule.OneTimeDateTime is { } executeAt
           && lastRun >= executeAt;

    // Why the whole trigger and not the cron string: an Interval or Once schedule carries no cron
    // expression, so comparing cron alone never saw its interval or execution time change. The time
    // zone is compared only for Cron -- Quartz reports a simple trigger in UTC whatever the row says.
    private static bool IsSameTrigger(IGenericSchedule live, IGenericSchedule desired)
        => string.Equals(live.TriggerType, desired.TriggerType, StringComparison.Ordinal)
           && string.Equals(live.CronExpression, desired.CronExpression, StringComparison.Ordinal)
           && live.IntervalSeconds == desired.IntervalSeconds
           && live.ExecuteAtUtc == desired.ExecuteAtUtc
           && (!string.Equals(desired.TriggerType, nameof(Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations.Cron), StringComparison.Ordinal)
               || string.Equals(live.TimeZoneId, desired.TimeZoneId, StringComparison.Ordinal));

    private static ReconciledSchedule ToGenericSchedule(IScheduleImplementationConfiguration schedule)
        => new ReconciledSchedule(
            scheduleId: schedule.Name,
            scheduleName: schedule.Name,
            // The pipeline's id, not its name (FDW-1088): the run resolves exactly this pipeline.
            processId: schedule.PipelineId.ToString("D", System.Globalization.CultureInfo.InvariantCulture),
            triggerType: schedule.ScheduleType,
            cronExpression: schedule.CronExpression,
            intervalSeconds: schedule.IntervalSeconds,
            executeAtUtc: schedule.OneTimeDateTime?.UtcDateTime,
            isActive: schedule.IsEnabled,
            timeZoneId: schedule.TimeZoneId,
            nextExecution: schedule.NextRunTime?.UtcDateTime);

    private sealed class ReconciledSchedule : IGenericSchedule
    {
        public ReconciledSchedule(
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
}
