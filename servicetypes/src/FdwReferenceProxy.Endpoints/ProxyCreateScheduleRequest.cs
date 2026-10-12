using Fdw.Services.Pipelines.Clients.Abstractions;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Request for creating a schedule via proxy.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ProxyCreateScheduleRequest
{
    /// <summary>
    /// Gets or sets the schedule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical id of the pipeline to schedule (FDW-1088). It is resolved in the caller's view; its
    /// name is looked up, not sent.
    /// </summary>
    public Guid PipelineId { get; set; }

    /// <summary>
    /// Gets or sets the scheduler type (Cron, Interval, Manual).
    /// </summary>
    public string SchedulerType { get; set; } = "Cron";

    /// <summary>
    /// Gets or sets the cron expression.
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Gets or sets the interval in seconds.
    /// </summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets the one-time execution date and time for Once-type schedules.
    /// </summary>
    public DateTimeOffset? OneTimeDateTime { get; set; }

    /// <summary>
    /// Gets or sets the triggering event name for Event-type schedules.
    /// </summary>
    public string? EventName { get; set; }

    /// <summary>
    /// Gets or sets the IANA time zone the schedule's times are evaluated in (FDW-1170), e.g. "America/Chicago". Omitted leaves
    /// the choice to the scheduler; a zone the server cannot resolve is refused naming the value.
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Gets or sets whether the schedule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the run parameter values the scheduler passes to the pipeline on each run (contract v2 item 9),
    /// validated against the pipeline's declared parameters exactly like a trigger.
    /// </summary>
    public IList<PipelineParameterValue>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the roles the schedule's runs act with (FDW-1161), by name. Required: at least one, only roles the caller
    /// holds in the tenant it acts in.
    /// </summary>
    public IList<string>? RunRoles { get; set; }
}
