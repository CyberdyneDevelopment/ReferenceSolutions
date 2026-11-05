using System;
using System.Threading;
using Fdw.Results;
using FdwReferenceSchedulers.Quartz.Logging;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FdwReferenceSchedulers.Quartz;

/// <summary>What a cron schedule's fire times between its last run and now came to (FDW-3788).</summary>
/// <param name="Count">How many fire times passed.</param>
/// <param name="First">The first of them; none when there were none.</param>
/// <param name="Latest">The latest of them: the time a catch-up run is due; none when there were none.</param>
public sealed record MissedCronFireTimes(int Count, DateTimeOffset? First, DateTimeOffset? Latest);

/// <summary>
/// Works out the cron fire times a schedule missed (FDW-3788): the times its expression names after its last run and up to now, in the schedule's time zone.
/// </summary>
/// <remarks>
/// Why the schedule's own last run and not a record of the scheduler's: Quartz here keeps its triggers in memory, so it knows nothing of what it missed, and the schedule
/// row already records when it last ran (<c>LastRunTime</c>, written by every fire). A run that someone triggered by hand counts as a run: the times after it are the ones
/// missed. A schedule that never ran has no last run and the caller says so; this is never asked about it.
/// </remarks>
public static class MissedCronFires
{
    /// <summary>Finds the fire times after <paramref name="lastRun"/> that are not after <paramref name="now"/>.</summary>
    /// <param name="scheduleId">The schedule, for the log.</param>
    /// <param name="cronExpression">The schedule's cron expression.</param>
    /// <param name="timeZoneId">The time zone the expression is read in.</param>
    /// <param name="lastRun">When the schedule last ran.</param>
    /// <param name="now">The moment the scheduler started, or the schedule was registered.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">Stops the count; a schedule that fires every second and was down for a year has many.</param>
    /// <returns>The fire times that passed; a failure when the expression or the zone cannot be read.</returns>
    public static IGenericResult<MissedCronFireTimes> Find(
        string scheduleId, string cronExpression, string timeZoneId, DateTimeOffset lastRun, DateTimeOffset now, ILogger logger, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleId);
        ArgumentNullException.ThrowIfNull(logger);
        CronExpression cron;
        try
        {
            cron = new CronExpression(cronExpression) { TimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId) };
        }
        catch (Exception ex) when (ex is FormatException or TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
        {
            return GenericResult<MissedCronFireTimes>.Failure(QuartzSchedulingLog.MissedFiresNotWorkedOut(logger, scheduleId, ex.Message));
        }

        var count = 0;
        DateTimeOffset? first = null;
        DateTimeOffset? latest = null;
        var next = cron.GetTimeAfter(lastRun);
        while (next is { } due && due <= now)
        {
            cancellationToken.ThrowIfCancellationRequested();
            count++;
            if (first is null)
            {
                first = due;
            }

            latest = due;
            next = cron.GetTimeAfter(due);
        }

        return GenericResult<MissedCronFireTimes>.Success(new MissedCronFireTimes(count, first, latest));
    }
}
