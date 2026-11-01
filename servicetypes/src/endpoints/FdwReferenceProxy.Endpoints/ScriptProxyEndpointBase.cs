using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// What the stored script forwarding endpoints share (FDW-1489): they take no typed request, forward as they were called, and
/// answer 502 when the ETL server cannot be reached. The policy is the ETL server's own, so a caller without it is refused there.
/// </summary>
public abstract class ScriptProxyEndpointBase : EndpointWithoutRequest
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ScriptProxyEndpointBase"/> class.</summary>
    /// <param name="factory">Supplies the named client that reaches the ETL server.</param>
    /// <param name="logger">The logger.</param>
    protected ScriptProxyEndpointBase(IHttpClientFactory factory, ILogger logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <summary>Gets the policy the caller must hold; the ETL server checks it again.</summary>
    protected abstract string RequiredPolicy { get; }

    /// <summary>Gets the HTTP method forwarded.</summary>
    protected abstract System.Net.Http.HttpMethod Method { get; }

    /// <summary>Gets the ETL server's path for this request, without a leading slash.</summary>
    protected abstract string TargetPath();

    /// <summary>Sets the route, the verb and the summary.</summary>
    /// <param name="summary">The one-line summary.</param>
    /// <param name="description">What the endpoint forwards.</param>
    protected void ConfigureScripts(string summary, string description)
    {
        Policies(RequiredPolicy);
        Summary(s =>
        {
            s.Summary = summary;
            s.Description = description;
        });
        Tags("Scripts");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var relayed = await EtlForwarder.Forward(HttpContext, _factory, _logger, Method, TargetPath(), ct).ConfigureAwait(false);
        if (relayed)
        {
            return;
        }

        AddError("Failed to proxy request to EtlServer");
        await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
    }
}
