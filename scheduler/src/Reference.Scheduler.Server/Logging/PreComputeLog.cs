using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Scheduler.Server.Logging;

/// <summary>
/// MessageLogging for pre-compute job operations.
/// EventId range: 1800-1849
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class PreComputeLog
{
    [MessageLogging(
        EventId = 1800,
        Level = LogLevel.Information,
        Message = "Pre-compute job started, running every {intervalMinutes} minutes")]
    public static partial IGenericMessage JobStarted(ILogger logger, int intervalMinutes);

    [MessageLogging(
        EventId = 1801,
        Level = LogLevel.Information,
        Message = "Pre-compute job disabled")]
    public static partial IGenericMessage JobDisabled(ILogger logger);

    [MessageLogging(
        EventId = 1802,
        Level = LogLevel.Warning,
        Message = "Pre-compute job skipped - previous run still executing")]
    public static partial IGenericMessage JobSkippedStillRunning(ILogger logger);

    [MessageLogging(
        EventId = 1803,
        Level = LogLevel.Information,
        Message = "Found {count} stale calculations to pre-compute")]
    public static partial IGenericMessage FoundStaleCalculations(ILogger logger, int count);

    [MessageLogging(
        EventId = 1804,
        Level = LogLevel.Debug,
        Message = "Pre-computed calculation '{calculationType}' in {elapsedMs}ms")]
    public static partial IGenericMessage CalculationPreComputed(ILogger logger, string calculationType, long elapsedMs);

    [MessageLogging(
        EventId = 1805,
        Level = LogLevel.Error,
        Message = "Pre-compute failed for '{calculationType}': {error}")]
    public static partial IGenericMessage PreComputeFailed(ILogger logger, string calculationType, string error);

    [MessageLogging(
        EventId = 1806,
        Level = LogLevel.Error,
        Message = "Pre-compute exception for '{calculationType}'")]
    public static partial IGenericMessage PreComputeException(ILogger logger, string calculationType);

    [MessageLogging(
        EventId = 1807,
        Level = LogLevel.Information,
        Message = "Pre-compute job completed - Success: {successCount}, Failed: {failureCount}")]
    public static partial IGenericMessage JobCompleted(ILogger logger, int successCount, int failureCount);

    [MessageLogging(
        EventId = 1808,
        Level = LogLevel.Information,
        Message = "Pre-compute job stopped")]
    public static partial IGenericMessage JobStopped(ILogger logger);

    [MessageLogging(
        EventId = 1809,
        Level = LogLevel.Debug,
        Message = "Recorded usage for calculation hash '{hash}', execution time: {executionMs}ms")]
    public static partial IGenericMessage UsageRecorded(ILogger logger, string hash, int executionMs);

    [MessageLogging(
        EventId = 1810,
        Level = LogLevel.Error,
        Message = "Failed to get stale calculations: {error}")]
    public static partial IGenericMessage FailedToGetStaleCalculations(ILogger logger, string error);

    [MessageLogging(
        EventId = 1811,
        Level = LogLevel.Information,
        Message = "Pre-compute job is starting first run after {delayMinutes} minute delay")]
    public static partial IGenericMessage JobFirstRun(ILogger logger, int delayMinutes);

    [MessageLogging(
        EventId = 1812,
        Level = LogLevel.Debug,
        Message = "Throttling between calculations: {delayMs}ms")]
    public static partial IGenericMessage Throttling(ILogger logger, int delayMs);

    [MessageLogging(
        EventId = 1813,
        Level = LogLevel.Error,
        Message = "Pre-compute job failed with unexpected error")]
    public static partial IGenericMessage JobFailedUnexpectedError(ILogger logger, Exception exception);

    [MessageLogging(
        EventId = 1814,
        Level = LogLevel.Trace,
        Message = "Pre-compute job executing - no stale calculation tracking configured")]
    public static partial IGenericMessage JobExecutingNoTracking(ILogger logger);

    [MessageLogging(
        EventId = 1815,
        Level = LogLevel.Warning,
        Message = "Failed to record pre-compute run stats: {error}")]
    public static partial IGenericMessage FailedToRecordRunStats(ILogger logger, string error);

    [MessageLogging(
        EventId = 1816,
        Level = LogLevel.Warning,
        Message = "Failed to update LastCachedAt for '{calculationHash}': {error}")]
    public static partial IGenericMessage FailedToUpdateCache(ILogger logger, string calculationHash, string error);

    [MessageLogging(
        EventId = 1817,
        Level = LogLevel.Error,
        Message = "Operation failed with no error message")]
    public static partial IGenericMessage OperationFailedNoMessage(ILogger logger);

    /// <summary>
    /// Returns the error message from a result, logging a warning if CurrentMessage is null.
    /// </summary>
    public static string GetError(Fdw.Results.IGenericResult result, ILogger logger)
    {
        if (result.CurrentMessage != null)
        {
            return result.CurrentMessage;
        }

        OperationFailedNoMessage(logger);
        return "Operation failed with no error message";
    }
}
