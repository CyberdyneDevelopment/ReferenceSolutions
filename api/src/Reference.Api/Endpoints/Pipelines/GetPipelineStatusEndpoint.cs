using Fdw.Services.Etl;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to get the status of a specific pipeline.
/// Sealed closure of generic base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetPipelineStatusEndpoint : GetPipelineStatusEndpointBase
{
    private readonly IFdwServiceProvider<IEtlPipeline, PipelineConfiguration> _pipelineProvider;
    private readonly ILogger<GetPipelineStatusEndpoint> _logger;

    /// <inheritdoc />
    public GetPipelineStatusEndpoint(
        IFdwServiceProvider<IEtlPipeline, PipelineConfiguration> pipelineProvider,
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

        return new GetPipelineStatusResponse
        {
            Found = pipelineResult.IsSuccess,
            Message = pipelineResult.IsSuccess ? "Ready" : "Not Found",
            Pipeline = pipelineResult.IsSuccess
                ? new PipelineStatusInfo
                {
                    Name = request.Name,
                    PipelineType = "Unknown",
                    IsExecuting = false
                }
                : null
        };
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
