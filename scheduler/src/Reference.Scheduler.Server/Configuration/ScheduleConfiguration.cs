using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Reference.Scheduler.Server.Configuration;

/// <summary>
/// Configuration for schedule definitions that control when and how pipelines are executed.
/// </summary>
/// <remarks>
/// <para>
/// This configuration defines the scheduling parameters for pipeline execution,
/// including the schedule type (Cron, Interval, Manual, OneTime), timing expressions,
/// and execution tracking information.
/// </para>
/// <para>
/// When the [ManagedConfiguration] source generator is enabled, it creates:
/// <list type="bullet">
/// <item><description>Table: sched.Schedule with all scheduling properties</description></item>
/// <item><description>Generated validator for required fields and constraints</description></item>
/// </list>
/// </para>
/// <para>
/// Example configuration from database or appsettings:
/// <code>
/// {
///   "Schedule": {
///     "Name": "DailyEtlJob",
///     "PipelineName": "SalesDataPipeline",
///     "ServiceOptionType": "Cron",
///     "CronExpression": "0 2 * * *",
///     "TimeZoneId": "UTC",
///     "IsEnabled": true
///   }
/// }
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[ManagedConfiguration(ServiceCategory = "Schedule")]
public sealed partial class ScheduleConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this schedule.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the unique name of the schedule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the configuration section name for this schedule.
    /// </summary>
    public string SectionName => "Schedules";

    /// <summary>
    /// Gets or sets the name of the pipeline to execute when this schedule triggers.
    /// </summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of scheduler to use for this schedule.
    /// </summary>
    /// <remarks>
    /// Supported values:
    /// <list type="bullet">
    /// <item><description>Cron - Executes based on a cron expression</description></item>
    /// <item><description>Interval - Executes at fixed intervals</description></item>
    /// <item><description>Manual - Only executes when manually triggered</description></item>
    /// <item><description>OneTime - Executes once at a specified time</description></item>
    /// </list>
    /// </remarks>
    public string ServiceOptionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cron expression for Cron-type schedules.
    /// </summary>
    /// <remarks>
    /// Only applicable when ServiceOptionType is "Cron".
    /// Uses standard cron format: minute hour day month day-of-week
    /// Example: "0 2 * * *" = daily at 2:00 AM
    /// </remarks>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Gets or sets the interval in seconds for Interval-type schedules.
    /// </summary>
    /// <remarks>
    /// Only applicable when ServiceOptionType is "Interval".
    /// Specifies the number of seconds between executions.
    /// </remarks>
    public int? IntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets the time zone ID for schedule evaluation.
    /// </summary>
    /// <remarks>
    /// Uses IANA time zone identifiers (e.g., "America/New_York", "Europe/London").
    /// Default is "UTC".
    /// </remarks>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// Gets or sets a value indicating whether this schedule is enabled.
    /// </summary>
    /// <remarks>
    /// Disabled schedules will not trigger pipeline execution.
    /// Default is true.
    /// </remarks>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the timestamp of the last execution of this schedule.
    /// </summary>
    /// <remarks>
    /// Updated automatically by the scheduler after each execution.
    /// Null if the schedule has never been executed.
    /// </remarks>
    public DateTimeOffset? LastRunTime { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the next scheduled execution.
    /// </summary>
    /// <remarks>
    /// Calculated by the scheduler based on ServiceOptionType and timing parameters.
    /// Null for Manual schedules or if the schedule is disabled.
    /// </remarks>
    public DateTimeOffset? NextRunTime { get; set; }
}
