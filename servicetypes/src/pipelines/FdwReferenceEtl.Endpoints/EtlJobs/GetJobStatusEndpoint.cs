using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceEtlJobs.Endpoints.Logging;
using Fdw.Services.Etl;

namespace FdwReferenceEtlJobs.Endpoints;

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
    private readonly IDataGatewayProvider _dataGateways;
    private readonly ILogger<GetJobStatusEndpoint> _logger;

    public GetJobStatusEndpoint(
        IExecutionTracker executionTracker,
        IDataGatewayProvider dataGateways,
        ILogger<GetJobStatusEndpoint> logger)
    {
        _executionTracker = executionTracker;
        _dataGateways = dataGateways;
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

        // The metrics row is enrichment and is already allowed to come back null, so an unnamed
        // operational store leaves it null rather than failing the request - but it says so, instead
        // of passing a null store name down to the gateway.
        EtlPipelineExecutionRecord? metrics = null;
        var operationalStore = EtlPipelineTypes.OperationalConnection;
        if (string.IsNullOrWhiteSpace(operationalStore))
        {
            EtlServerLog.EtlMetricsInsertFailed(
                _logger,
                req.ExecutionId,
                item.Name,
                $"{nameof(EtlPipelineTypes)}.{nameof(EtlPipelineTypes.OperationalConnection)} is not set; the host must name the operational store.");
        }
        else
        {
            // Why: fully qualified to avoid conflict with FastEndpoints Endpoint<T,R>.Query<T>()
            var queryCommand = Fdw.Commands.Data.Query.From<EtlPipelineExecutionRecord>(
                    operationalStore, "etl", "PipelineExecution")
                .Where("Id", req.ExecutionId)
                .Build();

            var metricsResult = await _dataGateways.ByName("Main").Execute<IEnumerable<EtlPipelineExecutionRecord>>(queryCommand, ct).ConfigureAwait(false);
            metrics = metricsResult.IsSuccess ? metricsResult.Value?.FirstOrDefault() : null;
        }

        long? durationMs = null;
        if (item.StartedAt.HasValue && item.CompletedAt.HasValue)
        {
            durationMs = (long)(item.CompletedAt.Value - item.StartedAt.Value).TotalMilliseconds;
        }

        var pipelineName = metrics != null ? metrics.PipelineName : item.Name;
        var recordsExtracted = metrics != null ? (long)metrics.RecordsExtracted : 0L;
        var recordsLoaded = metrics != null ? (long)metrics.RecordsLoaded : 0L;
        var recordsFailed = metrics != null ? (long)metrics.RecordsFailed : 0L;

        await Send.OkAsync(new JobStatusResponse(
            item.Id,
            pipelineName,
            item.State.Name,
            item.StartedAt?.DateTime,
            item.CompletedAt?.DateTime,
            durationMs,
            recordsExtracted,
            recordsLoaded,
            recordsFailed,
            item.ResultMessage), ct).ConfigureAwait(false);
    }
}
