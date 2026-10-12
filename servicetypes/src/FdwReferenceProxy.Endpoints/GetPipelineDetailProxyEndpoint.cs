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
/// Proxy endpoint to read one pipeline from the ETL server.
/// POST /api/v1/pipelines/get {name} → EtlServer POST /api/v1/pipelines/get {name}
/// </summary>
/// <remarks>
/// A read of one record is a POST, its name in the body: a GET is a plain paged list and nothing else (FDW-978).
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetPipelineDetailProxyEndpoint : Endpoint<PipelineNameProxyRequest, PipelineDetailResponse>
{
    private readonly IPipelineClient _client;
    private readonly ILogger<GetPipelineDetailProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetPipelineDetailProxyEndpoint"/> class.</summary>
    /// <param name="client">The client that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public GetPipelineDetailProxyEndpoint(IPipelineClient client, ILogger<GetPipelineDetailProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/pipelines/get");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "Get a pipeline (proxy)";
            s.Description = "Proxies the request to the ETL server to read one pipeline by name.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(PipelineNameProxyRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "POST", "Etl", "pipelines/get");

        var result = await _client.Get(req.Name, ct).ConfigureAwait(false);

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
