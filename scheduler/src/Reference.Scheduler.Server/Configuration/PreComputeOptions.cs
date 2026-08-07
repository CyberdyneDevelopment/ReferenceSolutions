using System.Diagnostics.CodeAnalysis;

namespace Reference.Scheduler.Server.Configuration;

/// <summary>
/// Configuration options for the pre-compute background job.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PreComputeOptions
{
    /// <summary>
    /// Gets or sets whether pre-computation is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval in minutes between pre-compute runs.
    /// </summary>
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets the initial delay in minutes before the first run.
    /// </summary>
    public int InitialDelayMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the threshold in minutes for considering a calculation stale.
    /// </summary>
    public int StalenessThresholdMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of calculations to pre-compute per run.
    /// </summary>
    public int MaxCalculationsPerRun { get; set; } = 50;

    /// <summary>
    /// Gets or sets the TTL in minutes for pre-computed results.
    /// </summary>
    public int PreComputedTtlMinutes { get; set; } = 120;

    /// <summary>
    /// Gets or sets the delay in milliseconds between calculations.
    /// </summary>
    public int DelayBetweenCalculationsMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the minimum execution count threshold for pre-computing.
    /// Calculations with fewer executions will be skipped.
    /// </summary>
    public int MinExecutionCountThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the API base URL for executing calculations.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://localhost:5001";
}
