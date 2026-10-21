using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Get the child executions of an execution (proxy).</summary>
[ExcludeFromCodeCoverage]
public sealed class GetExecutionChildrenProxyEndpoint : EndpointWithoutRequest
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<GetExecutionChildrenProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetExecutionChildrenProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public GetExecutionChildrenProxyEndpoint(IHttpClientFactory factory, ILogger<GetExecutionChildrenProxyEndpoint> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/etl/executions/{id}/children");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "Get the child executions of an execution (proxy)";
            s.Description = "Forwards to the ETL server, which owns executions. For a run of a graph pipeline the children are its stages.";
        });
        Tags("EtlOrchestration");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var relayed = await EtlGetForwarder.Forward(HttpContext, _factory, _logger, $"executions/{Route<Guid>("id")}/children", ct).ConfigureAwait(false);
        if (relayed)
            return;

        AddError("Failed to proxy request to EtlServer");
        await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
    }
}
