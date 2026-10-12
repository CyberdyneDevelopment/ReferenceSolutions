using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FdwReferenceProxy.Endpoints.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Proxy endpoint to create a pipeline on the ETL server.
/// POST /api/v1/pipelines -> EtlServer POST /api/v1/pipelines
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreatePipelineProxyEndpoint : Endpoint<CreatePipelineClientRequest, PipelineDetailResponse>
{
    private readonly IPipelineClient _client;
    private readonly ILogger<CreatePipelineProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="CreatePipelineProxyEndpoint"/> class.</summary>
    /// <param name="client">The client that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public CreatePipelineProxyEndpoint(IPipelineClient client, ILogger<CreatePipelineProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/pipelines");
        Policies("pipelines:write");
        Summary(s =>
        {
            s.Summary = "Create pipeline (proxy)";
            s.Description = "Proxies the request to the ETL server to create a new pipeline.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CreatePipelineClientRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "POST", "Etl", "pipelines");

        var result = await _client.CreatePipeline(req, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            ProxyLog.ProxyFailed(_logger, "Etl", result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            // A success with no value here is not expected on this path (see the client's own
            // non-success handling) -- kept as the prior fixed 502 rather than mapped, since
            // there is no real downstream status to relay for a call that itself succeeded.
            ProxyLog.ProxyFailed(_logger, "Etl", "Proxy request succeeded but returned no value");
            AddError("Failed to proxy request to EtlServer");
            await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Etl", 200);
        await Send.OkAsync(result.Value, ct).ConfigureAwait(false);
    }
}
