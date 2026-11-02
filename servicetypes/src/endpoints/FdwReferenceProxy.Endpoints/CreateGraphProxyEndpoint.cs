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
/// Proxies the request to the ETL server to create a graph pipeline.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateGraphProxyEndpoint : Endpoint<GraphPayload, GraphPayload>
{
    private readonly IPipelineClient _client;
    private readonly ILogger<CreateGraphProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="CreateGraphProxyEndpoint"/> class.</summary>
    /// <param name="client">The client that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public CreateGraphProxyEndpoint(IPipelineClient client, ILogger<CreateGraphProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/pipelines/graph");
        Policies("pipelines:write");
        Summary(s =>
        {
            s.Summary = "Create graph pipeline (proxy)";
            s.Description = "Proxies the request to the ETL server to create a graph pipeline.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(GraphPayload req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "POST", "Etl", "pipelines/graph");

        var result = await _client.CreateGraph(req, ct).ConfigureAwait(false);

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

        ProxyLog.ProxyResponse(_logger, "Etl", 201);
        await Send.ResponseAsync(result.Value, 201, ct).ConfigureAwait(false);
    }
}
