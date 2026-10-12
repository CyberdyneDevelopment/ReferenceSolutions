using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using FdwReferenceProxy.Endpoints.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Proxies an ETL execution approval to the ETL server.</summary>
public sealed class ApproveEtlExecutionProxyEndpoint : Endpoint<EtlExecutionProxyRequest>
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<ApproveEtlExecutionProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="ApproveEtlExecutionProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public ApproveEtlExecutionProxyEndpoint(
        IHttpClientFactory factory,
        ILogger<ApproveEtlExecutionProxyEndpoint> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/etl/executions/{id}/approve");
        Policies("pipelines:execute");
        Summary(s =>
        {
            s.Summary = "Approve a paused ETL execution (proxy)";
            s.Description = "Forwards to the ETL server, which owns the execution tracker and queue.";
        });
        Tags("EtlOrchestration");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(EtlExecutionProxyRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "POST", "Etl", $"etl/executions/{req.Id}/approve");

        var client = _factory.CreateClient(EtlOrchestrationProxy.ClientName);
        HttpResponseMessage response;
        try
        {
            response = await client
                .PostAsync($"etl/executions/{req.Id}/approve", content: null, ct)
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

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                ProxyLog.ProxyFailed(_logger, "Etl", $"status {(int)response.StatusCode}");

            await UpstreamPassThrough.Relay(HttpContext, response, body, ct).ConfigureAwait(false);
        }
    }
}
