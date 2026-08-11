using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ReferenceProxy.Endpoints.Logging;

namespace ReferenceProxy.Endpoints;

/// <summary>Proxies an ETL execution status read to the ETL server.</summary>
public sealed class GetEtlExecutionStatusProxyEndpoint : Endpoint<EtlExecutionProxyRequest>
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<GetEtlExecutionStatusProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetEtlExecutionStatusProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public GetEtlExecutionStatusProxyEndpoint(
        IHttpClientFactory factory,
        ILogger<GetEtlExecutionStatusProxyEndpoint> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/etl/executions/{id}");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "Get ETL execution status (proxy)";
            s.Description = "Forwards to the ETL server, which owns the execution status tree.";
        });
        Tags("EtlOrchestration");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(EtlExecutionProxyRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "GET", "Etl", $"etl/executions/{req.Id}");

        var client = _factory.CreateClient(EtlOrchestrationProxy.ClientName);
        using var response = await client
            .GetAsync($"etl/executions/{req.Id}", ct)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            ProxyLog.ProxyFailed(_logger, "Etl", $"status {(int)response.StatusCode}");

        HttpContext.Response.StatusCode = (int)response.StatusCode;
        HttpContext.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        await HttpContext.Response.WriteAsync(body, ct).ConfigureAwait(false);
    }
}
