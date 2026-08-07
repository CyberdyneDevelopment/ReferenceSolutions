using System.Diagnostics.CodeAnalysis;

namespace Reference.Scheduler.Server.Configuration;

/// <summary>
/// Local configuration for the scheduler background service.
/// Database-backed scheduler configuration (ScheduleContainerName, ConnectionName)
/// comes from Fdw.Services.Scheduling.SchedulerConfiguration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SchedulerConfiguration
{
    /// <summary>
    /// Gets or sets the interval in seconds between schedule evaluations.
    /// </summary>
    public int EvaluationIntervalSeconds { get; set; } = 60;
}
