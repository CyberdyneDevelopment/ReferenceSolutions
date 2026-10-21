using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Get the events of an execution (proxy).</summary>
[ExcludeFromCodeCoverage]
public sealed class GetExecutionEventsProxyEndpoint : EndpointWithoutRequest
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<GetExecutionEventsProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetExecutionEventsProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public GetExecutionEventsProxyEndpoint(IHttpClientFactory factory, ILogger<GetExecutionEventsProxyEndpoint> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/etl/executions/{id}/events");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "Get the events of an execution (proxy)";
            s.Description = "Forwards to the ETL server, which owns executions. For the stage of a graph pipeline the events say when each component started and ended and how many rows crossed each link.";
        });
        Tags("EtlOrchestration");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var relayed = await EtlGetForwarder.Forward(HttpContext, _factory, _logger, $"executions/{Route<Guid>("id")}/events", ct).ConfigureAwait(false);
        if (relayed)
            return;

        AddError("Failed to proxy request to EtlServer");
        await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
    }
}
