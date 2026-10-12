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
/// Proxy endpoint that checks a pipeline draft without saving it.
/// POST /api/v1/pipelines/validate -> EtlServer POST /api/v1/pipelines/validate
/// </summary>
/// <remarks>
/// Why a proxy: the console talks to the API host and the ETL server owns the pipeline checks. The body is the
/// create request's, so the draft is judged by exactly the checks creating it would run, run roles included.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ValidatePipelineProxyEndpoint : Endpoint<CreatePipelineClientRequest, ValidatePipelineResponse>
{
    private readonly IPipelineClient _client;
    private readonly ILogger<ValidatePipelineProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="ValidatePipelineProxyEndpoint"/> class.</summary>
    /// <param name="client">The client that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public ValidatePipelineProxyEndpoint(IPipelineClient client, ILogger<ValidatePipelineProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/pipelines/validate");
        Policies("pipelines:write");
        Summary(s =>
        {
            s.Summary = "Check a pipeline draft without saving it (proxy)";
            s.Description = "Proxies the draft to the ETL server, which answers every problem creating it would be refused for. An empty list means create would accept it.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CreatePipelineClientRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "POST", "Etl", "pipelines/validate");

        var result = await _client.ValidatePipeline(req, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            ProxyLog.ProxyFailed(_logger, "Etl", result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            // A success with no value here is not expected on this path; kept as the fixed 502 the other
            // pipeline proxies answer, since there is no downstream status to relay for a call that succeeded.
            ProxyLog.ProxyFailed(_logger, "Etl", "Proxy request succeeded but returned no value");
            AddError("Failed to proxy request to EtlServer");
            await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Etl", 200);
        await Send.OkAsync(result.Value, ct).ConfigureAwait(false);
    }
}
