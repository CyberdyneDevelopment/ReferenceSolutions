using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;
using FdwReferencePipelines.Endpoints.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to get the status of a specific pipeline.
/// Sealed closure of generic base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetPipelineStatusEndpoint : GetPipelineStatusEndpointBase
{
    private readonly IEtlPipelineProvider _pipelineProvider;
    private readonly ILogger<GetPipelineStatusEndpoint> _logger;

    /// <inheritdoc />
    public GetPipelineStatusEndpoint(
        IEtlPipelineProvider pipelineProvider,
        ILogger<GetPipelineStatusEndpoint> logger)
    {
        _pipelineProvider = pipelineProvider;
        _logger = logger;
    }

    /// <summary>Retrieves pipeline status by name.</summary>
    protected override async Task<GetPipelineStatusResponse> RetrieveStatus(GetPipelineStatusRequest request, CancellationToken ct)
    {
        PipelineLog.FetchingPipeline(_logger, request.Name);

        var pipelineResult = await _pipelineProvider.Get(request.Name, ct).ConfigureAwait(false);
        if (pipelineResult.IsFailure)
        {
            return new GetPipelineStatusResponse
            {
                Found = false,
                Message = "Not Found",
                Pipeline = null
            };
        }

        // Why the resolved pipeline's own type (FDW-899): the provider hands back the live pipeline,
        // which states what it is; this used to answer "Unknown" for every pipeline it found.
        using var pipeline = pipelineResult.Value!;
        return new GetPipelineStatusResponse
        {
            Found = true,
            Message = "Ready",
            Pipeline = new PipelineStatusInfo
            {
                Name = request.Name,
                PipelineType = pipeline.PipelineType,
                IsExecuting = false
            }
        };
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
