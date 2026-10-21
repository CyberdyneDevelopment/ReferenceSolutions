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
/// Proxies the request to the ETL server to read a graph pipeline.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetGraphProxyEndpoint : EndpointWithoutRequest<GraphPayload>
{
    private readonly IPipelineClient _client;
    private readonly ILogger<GetGraphProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetGraphProxyEndpoint"/> class.</summary>
    /// <param name="client">The client that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public GetGraphProxyEndpoint(IPipelineClient client, ILogger<GetGraphProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/pipelines/{Name}/graph");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "Get graph pipeline (proxy)";
            s.Description = "Proxies the request to the ETL server to read a graph pipeline.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var name = Route<string>("Name")!;
        ProxyLog.ProxyRequest(_logger, "GET", "Etl", $"pipelines/{name}/graph");

        var result = await _client.GetGraph(name, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            ProxyLog.ProxyFailed(_logger, "Etl", result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            ProxyLog.ProxyFailed(_logger, "Etl", "Proxy request succeeded but returned no value");
            AddError("Failed to proxy request to EtlServer");
            await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Etl", 200);
        await Send.OkAsync(result.Value, ct).ConfigureAwait(false);
    }
}
