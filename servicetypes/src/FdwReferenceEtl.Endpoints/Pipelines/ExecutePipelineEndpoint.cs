using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Notifications;
using Microsoft.Extensions.Logging;
using FdwReferencePipelines.Endpoints.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to execute an ETL pipeline.
/// Sealed closure of generic base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ExecutePipelineEndpoint : ExecutePipelineEndpointBase
{
    private readonly IEtlPipelineProvider _pipelineProvider;
    private readonly IPipelineStatusBroadcaster _statusBroadcaster;
    private readonly ILogger<ExecutePipelineEndpoint> _logger;

    /// <inheritdoc />
    public ExecutePipelineEndpoint(
        IEtlPipelineProvider pipelineProvider,
        IPipelineStatusBroadcaster statusBroadcaster,
        ILogger<ExecutePipelineEndpoint> logger)
    {
        _pipelineProvider = pipelineProvider;
        _statusBroadcaster = statusBroadcaster;
        _logger = logger;
    }

    /// <summary>Performs pipeline execution with logging and status broadcasting.</summary>
    protected override async Task<ExecutePipelineResponse> PerformExecution(ExecutePipelineRequest request, CancellationToken ct)
    {
        PipelineLog.FetchingPipeline(_logger, request.Name);

        // Get pipeline instance from provider
        var pipelineResult = await _pipelineProvider.Get(request.Name, ct);
        if (pipelineResult.IsFailure)
        {
            // Why null rather than a stand-in text (FDW-899): a failure that carries no message has none
            // to report, and "Unknown error" read as if it were the provider's own words.
            var error = pipelineResult.Messages.Count > 0
                ? string.Join("; ", pipelineResult.Messages.Select(m => m.Message))
                : null;
            PipelineLog.PipelineNotFound(_logger, request.Name);
            return new ExecutePipelineResponse
            {
                Success = false,
                NotFound = true,
                Message = error
            };
        }

        var pipeline = pipelineResult.Value!;
        var executionId = Guid.NewGuid();

        PipelineLog.RecordingExecutionStart(_logger, request.Name, executionId);

        // Execute pipeline
        try
        {
            var execResult = await pipeline.Execute(ct).ConfigureAwait(false);

            if (execResult.IsSuccess)
            {
                PipelineLog.PipelineRetrieved(_logger, $"{request.Name} completed successfully");
                await _statusBroadcaster.BroadcastStatusChange(request.Name, executionId, "Completed").ConfigureAwait(false);

                // Why null when the run reported nothing (FDW-899): zeros and the locally generated id
                // were stand-ins that read as real results. The id generated above is never handed to
                // the pipeline, so only the execution's own report can say which run it was.
                // Why the execution's own id and counts and not the one generated above: the
                // pipeline reports what it actually moved, and reporting zero next to a successful
                // run reads as "nothing was there to copy" — the one answer a caller cannot tell
                // from a working ingest without going to the database to check.
                var executed = execResult.Value;

                return new ExecutePipelineResponse
                {
                    Success = true,
                    ExecutionId = executed?.ExecutionId,
                    Message = "Pipeline executed successfully",
                    RecordsExtracted = executed?.RecordsExtracted,
                    RecordsTransformed = executed?.RecordsTransformed,
                    RecordsLoaded = executed?.RecordsLoaded,
                    RecordsFailed = executed?.RecordsFailed,
                    TotalDurationMs = executed?.TotalDuration.TotalMilliseconds
                };
            }
            else
            {
                var error = execResult.Messages.Count > 0
                    ? string.Join("; ", execResult.Messages.Select(m => m.Message))
                    : null;
                PipelineLog.PipelineUpdateFailed(_logger, request.Name, error);
                await _statusBroadcaster.BroadcastStatusChange(request.Name, executionId, "Failed", error).ConfigureAwait(false);

                // A failed run still moved whatever it moved before it stopped, and that is the
                // number that says which phase gave out.
                var partial = execResult.Value;

                return new ExecutePipelineResponse
                {
                    Success = false,
                    ExecutionId = partial?.ExecutionId,
                    Message = error,
                    RecordsExtracted = partial?.RecordsExtracted,
                    RecordsTransformed = partial?.RecordsTransformed,
                    RecordsLoaded = partial?.RecordsLoaded,
                    RecordsFailed = partial?.RecordsFailed,
                    TotalDurationMs = partial?.TotalDuration.TotalMilliseconds
                };
            }
        }
        catch (Exception ex)
        {
            PipelineLog.PipelineUpdateFailed(_logger, request.Name, ex.Message);
            await _statusBroadcaster.BroadcastStatusChange(request.Name, executionId, "Failed", "Pipeline execution failed. Check server logs for details.").ConfigureAwait(false);

            return new ExecutePipelineResponse
            {
                Success = false,
                ExecutionId = executionId,
                Message = "Pipeline execution failed. Check server logs for details."
            };
        }
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
