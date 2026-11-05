using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Proxy endpoint to list pipelines from the ETL server.
/// GET /api/v1/pipelines → EtlServer GET /api/v1/pipelines
/// </summary>
/// <remarks>
/// Pipelines had only their execution half proxied — TriggerPipelineProxy — while schedules had
/// their whole surface. Nothing else was missing: the ETL host already serves ListPipelinesEndpoint,
/// which pages. The console's Pipelines screen 404ed for want of this one file.
/// <para>
/// Why the query string is forwarded as it is (FDW-4101): this proxy read the pipelines through IPipelineClient.List, which
/// reads one page and answers its items as a bare list, so <c>skip</c> and <c>take</c> never reached the ETL host and the
/// route answered the same first rows to every request. The lookup and scripts proxies forward the query string untouched
/// (see <see cref="EtlGetForwarder"/>), so the ETL host, which owns pipelines and their paging settings, judges
/// <c>skip</c>, <c>take</c> and <c>tag</c> (FDW-3816) in one place and answers its page and its refusals with their codes.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetPipelinesProxyEndpoint : EndpointWithoutRequest
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<GetPipelinesProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetPipelinesProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public GetPipelinesProxyEndpoint(IHttpClientFactory factory, ILogger<GetPipelinesProxyEndpoint> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/pipelines");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "List pipelines (proxy)";
            s.Description = "Forwards the request (skip, take, tag) to the ETL server, which owns pipelines, and relays its page: skip, take, hasMore and items.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        // 502, not an empty list: a proxy that cannot reach its upstream has not learned that there
        // are no pipelines. The console renders those two differently and cannot tell them apart
        // from a 200 with nothing in it.
        var relayed = await EtlGetForwarder.Forward(HttpContext, _factory, _logger, "pipelines", ct).ConfigureAwait(false);
        if (relayed)
            return;

        AddError("Failed to proxy request to EtlServer");
        await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
    }
}
