using System;
using System.Diagnostics.CodeAnalysis;

namespace Reference.Scheduler.Server.Models;

/// <summary>
/// Information about a scheduled job.
/// </summary>
/// <param name="Name">The unique name of the schedule.</param>
/// <param name="PipelineName">The name of the pipeline to execute.</param>
/// <param name="ServiceOptionType">The type of scheduler (Cron, Interval, Manual, OneTime).</param>
/// <param name="CronExpression">The cron expression for Cron-type schedules.</param>
/// <param name="IntervalSeconds">The interval in seconds for Interval-type schedules.</param>
/// <param name="TimeZoneId">The time zone for schedule evaluation.</param>
/// <param name="IsEnabled">Indicates whether the schedule is enabled.</param>
/// <param name="LastRunTime">The timestamp of the last execution.</param>
/// <param name="NextRunTime">The next scheduled execution time, if available.</param>
[ExcludeFromCodeCoverage]
public sealed record ScheduleInfo(
    string Name,
    string PipelineName,
    string ServiceOptionType,
    string? CronExpression,
    int? IntervalSeconds,
    string TimeZoneId,
    bool IsEnabled,
    DateTimeOffset? LastRunTime,
    DateTimeOffset? NextRunTime);
