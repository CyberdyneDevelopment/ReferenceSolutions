using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Proxy endpoint to look up pipelines by name from the ETL server.
/// GET /api/v1/pipelines/lookup → EtlServer GET /api/v1/pipelines/lookup
/// </summary>
/// <remarks>
/// The lookup (FDW-1194) is served by the ETL host's own endpoint, and pipelines reach the public API host only through
/// proxies, so without this the route matched <c>/pipelines/{name}</c> and answered 405. The query string is forwarded
/// as it is (see <see cref="EtlGetForwarder"/>), so <c>q</c>, <c>match</c>, <c>id</c>, <c>skip</c>, <c>take</c> and
/// <c>total</c> are judged once, by the ETL host.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class LookupPipelinesProxyEndpoint : EndpointWithoutRequest
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<LookupPipelinesProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="LookupPipelinesProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public LookupPipelinesProxyEndpoint(IHttpClientFactory factory, ILogger<LookupPipelinesProxyEndpoint> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/pipelines/lookup");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "Look up pipelines by name (proxy)";
            s.Description = "Forwards the search (q, match, id, skip, take, total) to the ETL server, which owns pipelines, and relays its page of {id, name, implementation}.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var relayed = await EtlGetForwarder.Forward(HttpContext, _factory, _logger, "pipelines/lookup", ct).ConfigureAwait(false);
        if (relayed)
            return;

        AddError("Failed to proxy request to EtlServer");
        await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
    }
}
