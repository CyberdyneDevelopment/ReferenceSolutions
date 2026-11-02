using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// <c>POST /etl/executions/{id}/deny</c> (proxy, FDW-1740): denys a run that waits for approval. Forwards the call, with the caller's own token and the
/// optional message in the body, to the ETL server, which owns the execution tracker and the queue.
/// </summary>
/// <remarks>
/// Why <c>pipelines:approve</c> here too and not <c>pipelines:execute</c>: starting a run and releasing one that waits are different rights.
/// The ETL server checks the policy again, as the real person.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class DenyEtlExecutionProxyEndpoint : EndpointWithoutRequest
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<DenyEtlExecutionProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="DenyEtlExecutionProxyEndpoint"/> class.</summary>
    /// <param name="factory">Supplies the named client that reaches the ETL server.</param>
    /// <param name="logger">The logger.</param>
    public DenyEtlExecutionProxyEndpoint(IHttpClientFactory factory, ILogger<DenyEtlExecutionProxyEndpoint> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/etl/executions/{id}/deny");
        Policies("pipelines:approve");
        Summary(s =>
        {
            s.Summary = "Deny a run that waits for approval (proxy)";
            s.Description = "Forwards to the ETL server, which owns the execution tracker and queue. Send {} or {\"message\": \"...\"} as the body.";
        });
        Tags("EtlOrchestration");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        // Why "!": the route template is "/etl/executions/{id}/deny", so a request that reaches this handler always carries the segment.
        var id = Route<string>("id")!;
        var relayed = await EtlForwarder.Forward(
            HttpContext, _factory, _logger, System.Net.Http.HttpMethod.Post, $"executions/{System.Uri.EscapeDataString(id)}/deny", ct).ConfigureAwait(false);
        if (relayed)
        {
            return;
        }

        AddError("Failed to proxy request to EtlServer");
        await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
    }
}
