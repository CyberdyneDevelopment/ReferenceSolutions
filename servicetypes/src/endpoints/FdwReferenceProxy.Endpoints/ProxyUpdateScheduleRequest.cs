using Fdw.Services.Pipelines.Clients.Abstractions;
using System.Collections.Generic;
using System;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Request to update a schedule through the proxy. The name comes from the route; every other
/// property is optional so an update states only what it changes.
/// </summary>
public sealed class ProxyUpdateScheduleRequest
{
    /// <summary>Gets or sets the schedule name, bound from the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical id of the pipeline the schedule runs (FDW-1088), resolved in the caller's view.
    /// Omitted leaves it unchanged.
    /// </summary>
    public Guid? PipelineId { get; set; }

    /// <summary>Gets or sets the schedule kind (Cron, Interval, Once, Manual).</summary>
    public string? SchedulerType { get; set; }

    /// <summary>Gets or sets the cron expression for cron-based schedules.</summary>
    public string? CronExpression { get; set; }

    /// <summary>Gets or sets the interval in seconds for interval-based schedules.</summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>Gets or sets the time zone the schedule's times are expressed in.</summary>
    public string? TimeZoneId { get; set; }

    /// <summary>Gets or sets whether the schedule is enabled.</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the run parameter values the scheduler passes to the pipeline on each run (contract v2 item 9),
    /// validated against the pipeline's declared parameters exactly like a trigger.
    /// </summary>
    public IList<PipelineParameterValue>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the roles the schedule's runs act with (FDW-1161), by name. Null leaves the recorded roles standing; a list
    /// replaces them.
    /// </summary>
    public IList<string>? RunRoles { get; set; }
}
