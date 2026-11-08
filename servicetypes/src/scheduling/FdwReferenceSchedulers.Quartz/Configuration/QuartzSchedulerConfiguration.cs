using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Scheduling.Abstractions;

namespace FdwReferenceSchedulers.Quartz.Configuration;

/// <summary>
/// Configuration for the Quartz.NET scheduler implementation.
/// </summary>
/// <remarks>
/// Typed body under <c>sched.Scheduler</c> (the domain row) — carries only what Quartz itself
/// needs, including the storage location the domain root previously (wrongly) carried directly.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Scheduler", ServiceType = "Quartz")]
public sealed partial class QuartzSchedulerConfiguration : ISchedulerImplementationConfiguration
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

    /// <summary>Gets or sets the Quartz scheduler's name (FDW-4293: applied as the engine's <c>SchedulerName</c> when the host starts; it is not a cluster member id).</summary>
    public string InstanceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the size of Quartz's thread pool: how many fires run at once (FDW-4293: applied when the host starts).</summary>
    public int ThreadCount { get; set; }

    /// <summary>Gets or sets whether Quartz clusters. It must be false (FDW-4293): the scheduler runs on Quartz's in-memory job store, which clustering cannot share, and the validator and the engine start refuse true.</summary>
    public bool EnableClustering { get; set; }
}
