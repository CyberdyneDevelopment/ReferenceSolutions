using System.Net.Http;
using System.Net.Http.Json;
using Fdw.Services.Etl.Projects.Clients;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using FdwReferenceProxy.Endpoints.Logging;

namespace FdwReferenceProxy.Endpoints;

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
        HttpResponseMessage response;
        try
        {
            response = await client
                // Why a body (FDW-899): the trigger endpoint resolves its target from Id/Name and
                // records the TriggerSource; posting nothing was refused on every call.
                .PostAsJsonAsync($"etl/trigger/{req.Type}", new TriggerRequest
                {
                    Id = req.Id,
                    Name = req.Name,
                    ParentPath = req.ParentPath,
                    TriggerSource = req.TriggerSource,
                }, ct)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            // Same fix as GetEtlExecutionStatusProxyEndpoint: this raw-HttpClient pass-through has
            // no typed client to catch a connection failure for it, so it surfaced as a raw 500
            // instead of the 502 every other proxy endpoint returns when unreachable.
            ProxyLog.ProxyFailed(_logger, "Etl", ex.Message);
            AddError("Failed to proxy request to EtlServer");
            await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
            return;
        }

        await SendUpstream(response, ct).ConfigureAwait(false);
    }

    private async Task SendUpstream(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            ProxyLog.ProxyFailed(_logger, "Etl", $"status {(int)response.StatusCode}");

        await UpstreamPassThrough.Relay(HttpContext, response, body, ct).ConfigureAwait(false);
    }
}
