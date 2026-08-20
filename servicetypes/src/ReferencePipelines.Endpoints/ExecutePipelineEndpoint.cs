using Fdw.Services.Etl;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Notifications;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;
using ReferencePipelines.Endpoints.Logging;

namespace ReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to execute an ETL pipeline.
/// Sealed closure of generic base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ExecutePipelineEndpoint : ExecutePipelineEndpointBase
{
    private readonly IFdwServiceProvider<IEtlPipeline, PipelineConfiguration> _pipelineProvider;
    private readonly IPipelineStatusBroadcaster _statusBroadcaster;
    private readonly ILogger<ExecutePipelineEndpoint> _logger;

    /// <inheritdoc />
    public ExecutePipelineEndpoint(
        IFdwServiceProvider<IEtlPipeline, PipelineConfiguration> pipelineProvider,
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
            var error = pipelineResult.Messages.Any()
                ? string.Join("; ", pipelineResult.Messages.Select(m => m.Message))
                : "Unknown error";
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

                // Why the execution's own id and counts and not the one generated above: the
                // pipeline reports what it actually moved, and reporting zero next to a successful
                // run reads as "nothing was there to copy" — the one answer a caller cannot tell
                // from a working ingest without going to the database to check.
                var executed = execResult.Value;

                return new ExecutePipelineResponse
                {
                    Success = true,
                    ExecutionId = executed?.ExecutionId ?? executionId,
                    Message = "Pipeline executed successfully",
                    RecordsExtracted = executed?.RecordsExtracted ?? 0,
                    RecordsTransformed = executed?.RecordsTransformed ?? 0,
                    RecordsLoaded = executed?.RecordsLoaded ?? 0,
                    RecordsFailed = executed?.RecordsFailed ?? 0,
                    TotalDurationMs = executed?.TotalDuration.TotalMilliseconds ?? 0
                };
            }
            else
            {
                var error = execResult.Messages.Any()
                    ? string.Join("; ", execResult.Messages.Select(m => m.Message))
                    : "Pipeline execution failed";
                PipelineLog.PipelineUpdateFailed(_logger, request.Name, error);
                await _statusBroadcaster.BroadcastStatusChange(request.Name, executionId, "Failed", error).ConfigureAwait(false);

                // A failed run still moved whatever it moved before it stopped, and that is the
                // number that says which phase gave out.
                var partial = execResult.Value;

                return new ExecutePipelineResponse
                {
                    Success = false,
                    ExecutionId = partial?.ExecutionId ?? executionId,
                    Message = error,
                    RecordsExtracted = partial?.RecordsExtracted ?? 0,
                    RecordsTransformed = partial?.RecordsTransformed ?? 0,
                    RecordsLoaded = partial?.RecordsLoaded ?? 0,
                    RecordsFailed = partial?.RecordsFailed ?? 0,
                    TotalDurationMs = partial?.TotalDuration.TotalMilliseconds ?? 0
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
