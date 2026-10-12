using System;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions;
using FdwReferenceSchedulers.Quartz.Logging;
using Microsoft.Extensions.Logging;
using Quartz;
using TriggerKinds = Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations;

namespace FdwReferenceSchedulers.Quartz;

/// <summary>
/// Builds the Quartz trigger for a schedule from its trigger kind (FDW-902).
/// </summary>
/// <remarks>
/// Why one place: the create and update translators both need the same trigger, and a schedule used
/// to reach them as a cron expression only -- an Interval or Once schedule arrived as an empty cron
/// expression. Kinds are matched by the TriggerTypes option type names, not a registry lookup: the
/// TriggerTypes options are restricted to their own compilation, so ByName here would not find them.
/// </remarks>
public static class QuartzTriggers
{
    /// <summary>Whether Quartz registers a schedule of this kind (it fires on a clock).</summary>
    /// <param name="triggerType">The schedule's trigger kind.</param>
    /// <returns><c>true</c> for Cron, Interval and Once.</returns>
    public static bool IsClockDriven(string triggerType)
        => string.Equals(triggerType, nameof(TriggerKinds.Cron), StringComparison.Ordinal)
           || string.Equals(triggerType, nameof(TriggerKinds.Interval), StringComparison.Ordinal)
           || string.Equals(triggerType, nameof(TriggerKinds.Once), StringComparison.Ordinal);

    /// <summary>Builds the trigger for <paramref name="schedule"/>.</summary>
    /// <param name="schedule">The schedule.</param>
    /// <param name="triggerKey">The trigger's identity.</param>
    /// <param name="jobKey">The job the trigger fires; null when rescheduling, where Quartz keeps the existing job.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The trigger, or a failure naming what the schedule lacks.</returns>
    public static IGenericResult<ITrigger> Build(IGenericSchedule schedule, TriggerKey triggerKey, JobKey? jobKey, ILogger logger)
    {
        if (schedule is null) throw new ArgumentNullException(nameof(schedule));

        var builder = TriggerBuilder.Create().WithIdentity(triggerKey);
        if (jobKey is not null)
            builder = builder.ForJob(jobKey);

        if (string.Equals(schedule.TriggerType, nameof(TriggerKinds.Cron), StringComparison.Ordinal))
        {
            if (schedule.CronExpression is not { Length: > 0 } cron)
                return GenericResult<ITrigger>.Failure(QuartzSchedulingLog.TriggerDataMissing(logger, schedule.ScheduleId, schedule.TriggerType, nameof(schedule.CronExpression)));

            QuartzSchedulingLog.TriggerBuilt(logger, schedule.ScheduleId, schedule.TriggerType);
            return GenericResult<ITrigger>.Success(builder
                .WithCronSchedule(cron, x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId)))
                .Build());
        }

        if (string.Equals(schedule.TriggerType, nameof(TriggerKinds.Interval), StringComparison.Ordinal))
        {
            if (schedule.IntervalSeconds is not { } seconds || seconds <= 0)
                return GenericResult<ITrigger>.Failure(QuartzSchedulingLog.TriggerDataMissing(logger, schedule.ScheduleId, schedule.TriggerType, nameof(schedule.IntervalSeconds)));

            // Why StartNow: an interval schedule that has never run is due immediately, as the
            // deleted host loop treated it (IsDue with no last run).
            QuartzSchedulingLog.TriggerBuilt(logger, schedule.ScheduleId, schedule.TriggerType);
            return GenericResult<ITrigger>.Success(builder
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(seconds).RepeatForever())
                .Build());
        }

        if (string.Equals(schedule.TriggerType, nameof(TriggerKinds.Once), StringComparison.Ordinal))
        {
            if (schedule.ExecuteAtUtc is not { } executeAt)
                return GenericResult<ITrigger>.Failure(QuartzSchedulingLog.TriggerDataMissing(logger, schedule.ScheduleId, schedule.TriggerType, nameof(schedule.ExecuteAtUtc)));

            QuartzSchedulingLog.TriggerBuilt(logger, schedule.ScheduleId, schedule.TriggerType);
            return GenericResult<ITrigger>.Success(builder
                .StartAt(new DateTimeOffset(DateTime.SpecifyKind(executeAt, DateTimeKind.Utc)))
                .WithSimpleSchedule(x => x.WithRepeatCount(0))
                .Build());
        }

        return GenericResult<ITrigger>.Failure(QuartzSchedulingLog.ScheduleNotClockDriven(logger, schedule.ScheduleId, schedule.TriggerType));
    }
}
