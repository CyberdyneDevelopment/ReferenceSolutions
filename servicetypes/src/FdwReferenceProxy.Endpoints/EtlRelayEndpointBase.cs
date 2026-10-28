using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays one route of the ETL server through reference-api, which a browser can reach and the ETL server cannot be.</summary>
/// <remarks>
/// The same pass-through as the other relays over <see cref="EtlOrchestrationProxy.ClientName"/>: the query string and the body are forwarded as the
/// caller sent them, and the ETL server's status, content type and body come back unchanged, so a refusal keeps its code and reason. The caller needs the
/// permission the ETL endpoint declares. What each relay forwards is its <see cref="EtlRelay"/>; nothing is read into a request class, so the ETL
/// server stays the one place that judges the request.
/// </remarks>
public abstract class EtlRelayEndpointBase : EndpointWithoutRequest
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="EtlRelayEndpointBase"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger of the relaying endpoint.</param>
    /// <param name="relay">The route this endpoint relays.</param>
    protected EtlRelayEndpointBase(IHttpClientFactory factory, ILogger logger, EtlRelay relay)
    {
        _factory = factory;
        _logger = logger;
        Relay = relay;
    }

    /// <summary>Gets the route this endpoint relays.</summary>
    public EtlRelay Relay { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Verbs(Relay.Verb);
        Routes(Relay.Route);
        Policies(Relay.Policy);
        Summary(s =>
        {
            s.Summary = Relay.Summary;
            s.Description = Relay.Description;
        });
        Tags(Relay.Tag);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var relayed = await EtlForwarder.Forward(HttpContext, _factory, _logger, Relay.Method, Relay.UpstreamFor(name => Route<string>(name)), ct).ConfigureAwait(false);
        if (relayed)
        {
            return;
        }

        AddError("Failed to proxy request to EtlServer");
        await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
    }
}
