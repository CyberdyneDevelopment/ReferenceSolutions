using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceSchedulers.Hangfire.Logging;
using TriggerKinds = Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations;

namespace FdwReferenceSchedulers.Hangfire.Services;

/// <summary>
/// Reconciles Hangfire's live recurring-job set to <c>sched.Schedule</c>: configuration is the one
/// source of truth. Enabled and missing from the engine → create. Present but changed → update.
/// Disabled or no longer configured but still live → delete.
/// </summary>
/// <remarks>
/// Deleting rather than pausing is not a shortcut — Hangfire has no paused state. A recurring job
/// either exists and runs or does not exist, so "disabled" is expressed by removing it, and
/// re-enabling re-creates it from the full definition configuration still holds. That is also why
/// this never issues a Resume: there is nothing to resume from.
/// <para>
/// Runs on the inherited poll. The configuration providers raise no change notification (same
/// decision as the Quartz scheduler's own reconciliation service), so a schedule edit is picked up
/// on the next evaluation rather than immediately. <see cref="_evaluationLock"/> keeps an
/// overrunning evaluation from overlapping the next one against the same live job set.
/// </para>
/// </remarks>
public sealed class HangfireReconciliationService : SchedulerBackgroundServiceBase
{
    private readonly ILogger<HangfireReconciliationService> _logger;
    private readonly ScheduleConfigurationProvider _scheduleProvider;
    private readonly IDomainConfigurationProvider<ISchedulerImplementationConfiguration> _hangfireConfigProvider;
    private readonly IHangfireSchedulingFactory _factory;
    private readonly TimeSpan _interval;
    private readonly SemaphoreSlim _evaluationLock = new(1, 1);

    private int _lastEvaluatedCount;

    /// <summary>Initializes a new instance of the <see cref="HangfireReconciliationService"/> class.</summary>
    public HangfireReconciliationService(
        ScheduleConfigurationProvider scheduleProvider,
        IDomainConfigurationProvider<ISchedulerImplementationConfiguration> hangfireConfigProvider,
        IHangfireSchedulingFactory factory,
        IAuthenticationContextAccessor authenticationContextAccessor,
        TimeSpan interval,
        ILogger<HangfireReconciliationService>? logger = null)
        : base(authenticationContextAccessor, logger)
    {
        _scheduleProvider = scheduleProvider ?? throw new ArgumentNullException(nameof(scheduleProvider));
        _hangfireConfigProvider = hangfireConfigProvider ?? throw new ArgumentNullException(nameof(hangfireConfigProvider));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _interval = interval;
        _logger = logger ?? NullLogger<HangfireReconciliationService>.Instance;

        // Why no change subscription: the configuration providers raise no change notification, so
        // reconciliation is interval-driven. A schedule edit is picked up on the next evaluation
        // rather than immediately. Same decision as QuartzReconciliationService.
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
            // A pass is already running (poll and Changed landed together) — it will see whatever
            // just changed, so skipping is correct rather than a dropped update.
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

        var configsResult = await _hangfireConfigProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!configsResult.IsSuccess || configsResult.Value is null || configsResult.Value.Count == 0)
        {
            HangfireSchedulingLog.NativeCallFailed(_logger,
                new InvalidOperationException("No Hangfire scheduler configuration row found; nothing to reconcile against."),
                "(unknown)");
            return;
        }

        // Why the first row: this reference implementation expects exactly one active Hangfire
        // scheduler per host. Reconciling more than one would mean deciding which owns a given
        // schedule, which sched.Schedule has no column for today.
        var configuration = (ISchedulerImplementationConfiguration)configsResult.Value[0];

        var serviceResult = _factory.Create(configuration);
        if (!serviceResult.IsSuccess || serviceResult.Value is null)
        {
            HangfireSchedulingLog.NativeCallFailed(_logger,
                new InvalidOperationException(serviceResult.CurrentMessage ?? "Failed to build the Hangfire scheduling service."),
                "(unknown)");
            return;
        }

        var service = serviceResult.Value;

        var schedulesResult = await _scheduleProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!schedulesResult.IsSuccess || schedulesResult.Value is null)
        {
            HangfireSchedulingLog.NativeCallFailed(_logger,
                new InvalidOperationException(schedulesResult.CurrentMessage ?? "Failed to read sched.Schedule."),
                "(unknown)");
            return;
        }

        var liveResult = await service.GetSchedules(includeInactive: true, cancellationToken).ConfigureAwait(false);
        if (!liveResult.IsSuccess || liveResult.Value is null)
        {
            HangfireSchedulingLog.NativeCallFailed(_logger,
                new InvalidOperationException(liveResult.CurrentMessage ?? "Failed to read Hangfire's live recurring-job set."),
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
                // Why skipped (FDW-902): Hangfire's recurring jobs take only a cron expression; any
                // other kind reached here as an empty one.
                if (!string.Equals(schedule.ScheduleType, nameof(TriggerKinds.Cron), StringComparison.Ordinal))
                {
                    HangfireSchedulingLog.NotCronSchedule(_logger, schedule.Name, schedule.ScheduleType);
                    if (liveSchedule is not null)
                        await service.DeleteSchedule(schedule.Name, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                // AddOrUpdate is idempotent, so create covers both "missing" and "changed" — but
                // only issuing it on a real difference keeps the pass quiet when nothing moved.
                if (liveSchedule is null)
                {
                    await service.CreateSchedule(ToGenericSchedule(schedule), cancellationToken).ConfigureAwait(false);
                }
                else if (!string.Equals(liveSchedule.CronExpression, schedule.CronExpression, StringComparison.Ordinal)
                    || !string.Equals(liveSchedule.ProcessId, ToGenericSchedule(schedule).ProcessId, StringComparison.Ordinal))
                {
                    // A re-pointed schedule keeps its cron; a job left holding the old pipeline id would run the old
                    // pipeline (FDW-1088).
                    await service.UpdateSchedule(ToGenericSchedule(schedule), cancellationToken).ConfigureAwait(false);
                }
            }
            else if (liveSchedule is not null)
            {
                await service.DeleteSchedule(schedule.Name, cancellationToken).ConfigureAwait(false);
            }
        }

        var configuredNames = configured.Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var liveSchedule in live.Values)
        {
            if (!configuredNames.Contains(liveSchedule.ScheduleId))
            {
                await service.DeleteSchedule(liveSchedule.ScheduleId, cancellationToken).ConfigureAwait(false);
            }
        }

        _lastEvaluatedCount = evaluated;
    }

    private static ReconciledSchedule ToGenericSchedule(IScheduleImplementationConfiguration schedule)
        => new(
            scheduleId: schedule.Name,
            scheduleName: schedule.Name,
            // The pipeline's id, not its name (FDW-1088): the run resolves exactly this pipeline.
            processId: schedule.PipelineId.ToString("D", System.Globalization.CultureInfo.InvariantCulture),
            triggerType: schedule.ScheduleType,
            cronExpression: schedule.CronExpression,
            isActive: schedule.IsEnabled,
            timeZoneId: schedule.TimeZoneId,
            nextExecution: schedule.NextRunTime?.UtcDateTime);

    private sealed class ReconciledSchedule : IGenericSchedule
    {
        public ReconciledSchedule(
            string scheduleId, string scheduleName, string processId, string triggerType, string? cronExpression,
            bool isActive, string timeZoneId, DateTime? nextExecution)
        {
            ScheduleId = scheduleId;
            ScheduleName = scheduleName;
            ProcessId = processId;
            TriggerType = triggerType;
            CronExpression = cronExpression;
            IsActive = isActive;
            TimeZoneId = timeZoneId;
            NextExecution = nextExecution;
        }

        public string ScheduleId { get; }
        public string ScheduleName { get; }
        public string ProcessId { get; }
        public string TriggerType { get; }
        public string? CronExpression { get; }
        public int? IntervalSeconds => null;
        public DateTime? ExecuteAtUtc => null;
        public DateTime? NextExecution { get; }
        public bool IsActive { get; }
        public string TimeZoneId { get; }
        public IReadOnlyDictionary<string, object>? Metadata => null;
    }
}
