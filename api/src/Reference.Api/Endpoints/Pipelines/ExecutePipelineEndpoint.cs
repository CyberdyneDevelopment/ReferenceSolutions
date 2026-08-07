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
using Reference.Api.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to execute an ETL pipeline.
/// Sealed closure of generic base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecutePipelineEndpoint : ExecutePipelineEndpointBase
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

                return new ExecutePipelineResponse
                {
                    Success = true,
                    ExecutionId = executionId,
                    Message = "Pipeline executed successfully"
                };
            }
            else
            {
                var error = execResult.Messages.Any()
                    ? string.Join("; ", execResult.Messages.Select(m => m.Message))
                    : "Pipeline execution failed";
                PipelineLog.PipelineUpdateFailed(_logger, request.Name, error);
                await _statusBroadcaster.BroadcastStatusChange(request.Name, executionId, "Failed", error).ConfigureAwait(false);

                return new ExecutePipelineResponse
                {
                    Success = false,
                    ExecutionId = executionId,
                    Message = error
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
