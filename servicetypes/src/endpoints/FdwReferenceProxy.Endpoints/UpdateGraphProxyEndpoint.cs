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
/// Proxies the request to the ETL server to replace a graph pipeline whole.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateGraphProxyEndpoint : Endpoint<GraphPayload, GraphPayload>
{
    private readonly IPipelineClient _client;
    private readonly ILogger<UpdateGraphProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="UpdateGraphProxyEndpoint"/> class.</summary>
    /// <param name="client">The client that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public UpdateGraphProxyEndpoint(IPipelineClient client, ILogger<UpdateGraphProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Put("/pipelines/{Name}/graph");
        Policies("pipelines:write");
        Summary(s =>
        {
            s.Summary = "Replace graph pipeline (proxy)";
            s.Description = "Proxies the request to the ETL server to replace a graph pipeline whole.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(GraphPayload req, CancellationToken ct)
    {
        var name = Route<string>("Name")!;
        ProxyLog.ProxyRequest(_logger, "PUT", "Etl", $"pipelines/{name}/graph");

        var result = await _client.ReplaceGraph(name, req, ct).ConfigureAwait(false);

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
