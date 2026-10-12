using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using FdwReferenceProxy.Endpoints.Logging;

namespace FdwReferenceProxy.Endpoints;

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
        HttpResponseMessage response;
        try
        {
            response = await client
                .GetAsync($"etl/executions/{req.Id}", ct)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            // Every other proxy endpoint routes through a typed client that catches this itself
            // and returns a failed IGenericResult; this one and its two raw-HttpClient siblings
            // talk to EtlOrchestrationProxy.ClientName directly because the ETL server exposes no
            // typed-client method for these routes, which left a connection failure unhandled --
            // it surfaced as a raw 500 instead of the 502 every other proxy endpoint returns when
            // its downstream host is unreachable.
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
