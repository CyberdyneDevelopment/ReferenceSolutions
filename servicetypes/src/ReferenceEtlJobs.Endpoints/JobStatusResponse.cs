using System;
using System.Diagnostics.CodeAnalysis;

namespace ReferenceEtlJobs.Endpoints;

/// <summary>
/// Response model for job status.
/// </summary>
/// <param name="ExecutionId">The unique execution ID.</param>
/// <param name="PipelineName">The name of the pipeline.</param>
/// <param name="Status">Current execution status.</param>
/// <param name="StartedAt">When execution started.</param>
/// <param name="CompletedAt">When execution completed, if finished.</param>
/// <param name="DurationMs">Execution duration in milliseconds.</param>
/// <param name="RecordsExtracted">Number of records extracted.</param>
/// <param name="RecordsLoaded">Number of records loaded.</param>
/// <param name="RecordsFailed">Number of records that failed.</param>
/// <param name="ErrorMessage">Error message if failed.</param>
[ExcludeFromCodeCoverage]
public sealed record JobStatusResponse(
    Guid ExecutionId,
    string PipelineName,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    long? DurationMs,
    long RecordsExtracted,
    long RecordsLoaded,
    long RecordsFailed,
    string? ErrorMessage);
