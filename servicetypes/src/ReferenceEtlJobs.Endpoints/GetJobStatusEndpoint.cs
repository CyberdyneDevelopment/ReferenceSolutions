using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceEtlJobs.Endpoints.Logging;
using ReferenceEtlJobs.Endpoints;

namespace ReferenceEtlJobs.Endpoints;

/// <summary>
/// Deprecated: <c>GET /etl/jobs/{ExecutionId}/status</c> — flat pipeline status.
/// The canonical status endpoint is now <c>GET /etl/executions/{executionId}</c> which supports
/// both flat (pipeline) and hierarchical (project/stage/step) executions.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class JobStatusRequest
{
    public Guid ExecutionId { get; set; }
}

/// <summary>
/// Deprecated: use <c>GET /etl/executions/{executionId}</c> instead.
/// Kept for one release cycle for backward compatibility.
/// Returns flat pipeline execution status.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetJobStatusEndpoint : Endpoint<JobStatusRequest, JobStatusResponse>
{
    private readonly IExecutionTracker _executionTracker;
    private readonly IDataGateway _dataGateway;
    private readonly ILogger<GetJobStatusEndpoint> _logger;

    public GetJobStatusEndpoint(
        IExecutionTracker executionTracker,
        IDataGateway dataGateway,
        ILogger<GetJobStatusEndpoint> logger)
    {
        _executionTracker = executionTracker;
        _dataGateway = dataGateway;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<GetJobStatusEndpoint>.Instance;
    }

    public override void Configure()
    {
        // Why: legacy route alias for GET /etl/executions/{executionId}; secured like all data
        // endpoints (no anonymous access) — pipelines:read matches the execution-status endpoint.
        Get("etl/jobs/{ExecutionId}/status");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "[DEPRECATED] Get ETL job status";
            s.Description = "Deprecated alias. Use GET /etl/executions/{executionId} for hierarchical execution status.";
        });
    }

    public override async Task HandleAsync(JobStatusRequest req, CancellationToken ct)
    {
        EtlServerLog.JobStatusRequestReceived(_logger, req.ExecutionId);

        var itemResult = await _executionTracker.GetItem(req.ExecutionId, ct).ConfigureAwait(false);
        if (!itemResult.IsSuccess || itemResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var item = itemResult.Value;

        // Why: fully qualified to avoid conflict with FastEndpoints Endpoint<T,R>.Query<T>()
        var queryCommand = Fdw.Commands.Data.Query.From<EtlPipelineExecutionRecord>("EtlDb", "etl", "PipelineExecution")
            .Where("Id", req.ExecutionId)
            .Build();

        var metricsResult = await _dataGateway.Execute<IEnumerable<EtlPipelineExecutionRecord>>(queryCommand, ct).ConfigureAwait(false);
        var metrics = metricsResult.IsSuccess ? metricsResult.Value?.FirstOrDefault() : null;

        long? durationMs = null;
        if (item.StartedAt.HasValue && item.CompletedAt.HasValue)
        {
            durationMs = (long)(item.CompletedAt.Value - item.StartedAt.Value).TotalMilliseconds;
        }

        var pipelineName = metrics != null ? metrics.PipelineName : item.Name;
        var startedAt = item.StartedAt.HasValue ? item.StartedAt.Value.DateTime : item.CreatedAt.DateTime;
        var recordsExtracted = metrics != null ? (long)metrics.RecordsExtracted : 0L;
        var recordsLoaded = metrics != null ? (long)metrics.RecordsLoaded : 0L;
        var recordsFailed = metrics != null ? (long)metrics.RecordsFailed : 0L;

        await Send.OkAsync(new JobStatusResponse(
            item.Id,
            pipelineName,
            item.State.Name,
            startedAt,
            item.CompletedAt?.DateTime,
            durationMs,
            recordsExtracted,
            recordsLoaded,
            recordsFailed,
            item.ResultMessage), ct).ConfigureAwait(false);
    }
}
