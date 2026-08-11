using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ReferenceProxy.Endpoints.Logging;

namespace ReferenceProxy.Endpoints;

/// <summary>Proxies an orchestration-node trigger to the ETL server.</summary>
public sealed class TriggerOrchestrationNodeProxyEndpoint : Endpoint<TriggerOrchestrationNodeProxyRequest>
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<TriggerOrchestrationNodeProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="TriggerOrchestrationNodeProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public TriggerOrchestrationNodeProxyEndpoint(
        IHttpClientFactory factory,
        ILogger<TriggerOrchestrationNodeProxyEndpoint> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/etl/trigger/{type}");
        Policies("pipelines:execute");
        Summary(s =>
        {
            s.Summary = "Trigger an orchestration node (proxy)";
            s.Description = "Forwards the trigger to the ETL server, which owns the orchestration queue.";
        });
        Tags("EtlOrchestration");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(TriggerOrchestrationNodeProxyRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "POST", "Etl", $"etl/trigger/{req.Type}");

        var client = _factory.CreateClient(EtlOrchestrationProxy.ClientName);
        using var response = await client
            .PostAsync($"etl/trigger/{req.Type}", content: null, ct)
            .ConfigureAwait(false);

        await SendUpstream(response, ct).ConfigureAwait(false);
    }

    private async Task SendUpstream(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            ProxyLog.ProxyFailed(_logger, "Etl", $"status {(int)response.StatusCode}");

        HttpContext.Response.StatusCode = (int)response.StatusCode;
        HttpContext.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        await HttpContext.Response.WriteAsync(body, ct).ConfigureAwait(false);
    }
}
