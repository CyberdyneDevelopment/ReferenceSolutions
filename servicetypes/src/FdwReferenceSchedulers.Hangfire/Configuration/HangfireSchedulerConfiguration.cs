using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Scheduling.Abstractions;

namespace FdwReferenceSchedulers.Hangfire.Configuration;

/// <summary>
/// Configuration for the Hangfire scheduler implementation.
/// </summary>
/// <remarks>
/// Typed body under <c>sched.Scheduler</c> (the domain row) — carries only what Hangfire itself
/// needs, including the storage location the domain root previously (wrongly) carried directly.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Scheduler", ServiceType = "Hangfire")]
public sealed partial class HangfireSchedulerConfiguration : ISchedulerImplementationConfiguration
{
    // Why: NO Guid.NewGuid() default — the provider mints this before INSERT via CreateVersion7().
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string ScheduleContainerName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string DataStoreName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string PathName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the configured connection Hangfire's own storage lives in (e.g. the
    /// connection naming OpsDb). Named, not carried: the connection's settings and its secret are
    /// resolved through the connection provider like every other connection, and the composed
    /// connection string never leaves the implementation.
    /// </summary>
    public string StorageConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the size of Hangfire's worker pool.</summary>
    public int WorkerCount { get; set; } = 5;
}
