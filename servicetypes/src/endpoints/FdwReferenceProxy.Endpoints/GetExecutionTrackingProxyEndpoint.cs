using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Get what a run did (proxy).</summary>
[ExcludeFromCodeCoverage]
public sealed class GetExecutionTrackingProxyEndpoint : EndpointWithoutRequest
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<GetExecutionTrackingProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetExecutionTrackingProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public GetExecutionTrackingProxyEndpoint(IHttpClientFactory factory, ILogger<GetExecutionTrackingProxyEndpoint> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/etl/executions/{id}/tracking");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "Get what a run did (proxy)";
            s.Description = "Forwards to the ETL server, which owns executions. Returns the rows read, written and rejected, the size moved and the run time of the run, each stage and each component. A figure the run's records do not hold is null.";
        });
        Tags("EtlOrchestration");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var relayed = await EtlGetForwarder.Forward(HttpContext, _factory, _logger, $"executions/{Route<Guid>("id")}/tracking", ct).ConfigureAwait(false);
        if (relayed)
            return;

        AddError("Failed to proxy request to EtlServer");
        await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
    }
}
