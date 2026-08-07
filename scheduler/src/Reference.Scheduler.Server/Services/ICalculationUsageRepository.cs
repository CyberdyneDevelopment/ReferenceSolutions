using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Reference.Scheduler.Server.Services;

/// <summary>
/// Repository for accessing calculation usage data.
/// </summary>
public interface ICalculationUsageRepository
{
    /// <summary>
    /// Gets stale, high-value calculations that should be pre-computed.
    /// </summary>
    /// <param name="minExecutionCount">Minimum execution count threshold.</param>
    /// <param name="stalenessThresholdMinutes">Minutes since last cache before considered stale.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of stale calculation usage records.</returns>
    Task<IGenericResult<IReadOnlyList<CalculationUsageRecord>>> GetStaleCalculations(
        int minExecutionCount,
        int stalenessThresholdMinutes,
        int maxResults,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the LastCachedAt timestamp for a calculation after pre-computing.
    /// </summary>
    /// <param name="calculationHash">The calculation hash to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<IGenericResult> UpdateLastCachedAt(string calculationHash, CancellationToken cancellationToken);

    /// <summary>
    /// Records a pre-compute run in the PreComputeSchedule table.
    /// </summary>
    /// <param name="successCount">Number of successful calculations.</param>
    /// <param name="failureCount">Number of failed calculations.</param>
    /// <param name="calculationsProcessed">Total calculations processed.</param>
    /// <param name="durationMs">Total duration in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<IGenericResult> RecordPreComputeRun(
        int successCount,
        int failureCount,
        int calculationsProcessed,
        long durationMs,
        CancellationToken cancellationToken);
}

/// <summary>
/// Record representing a calculation usage entry from cfg.CalculationUsage.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CalculationUsageRecord
{
    public Guid RowId { get; set; }
    public string CalculationType { get; set; } = string.Empty;
    public string CalculationHash { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public long AverageDurationMs { get; set; }
    public DateTime? LastCachedAt { get; set; }
}
