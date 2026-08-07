using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints.EtlOrchestration;

/// <summary>
/// Request for triggering an orchestration node by type.
/// </summary>
public sealed class TriggerOrchestrationNodeProxyRequest
{
    /// <summary>Gets or sets the orchestration node type to trigger.</summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Request identifying a single ETL execution.
/// </summary>
public sealed class EtlExecutionProxyRequest
{
    /// <summary>Gets or sets the execution identifier.</summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Forwards ETL orchestration calls to the ETL server.
/// </summary>
/// <remarks>
/// <para>
/// Why these are proxies and not implementations: orchestration is executed by the ETL server, which
/// owns the <c>OrchestrationNodeExecutionQueue</c> and the background consumer that drains it. That
/// queue is an in-memory Channel, so a host that registers it runs its OWN orchestrator over its OWN
/// queue. These endpoints previously did exactly that — enqueuing in the API process — which meant
/// which host executed a trigger depended on which host received the request.
/// </para>
/// <para>
/// The routes are identical to the ETL server's, so each handler is a straight pass-through. The
/// caller's bearer token rides along via <c>BearerTokenHandler</c> on the named client.
/// </para>
/// </remarks>
internal static class EtlOrchestrationProxy
{
    /// <summary>The named HttpClient registered by PipelineJobClientType from ApiClients:PipelineJobClient:BaseUrl.</summary>
    internal const string ClientName = "PipelineJobClient";
}

/// <summary>Proxies an orchestration-node trigger to the ETL server.</summary>
public sealed class TriggerOrchestrationNodeProxyEndpoint : Endpoint<TriggerOrchestrationNodeProxyRequest>
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<TriggerOrchestrationNodeProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="TriggerOrchestrationNodeProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public TriggerOrchestrationNodeProxyEndpoint(
        IHttpClientFactory factory,
        ILogger<TriggerOrchestrationNodeProxyEndpoint> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/etl/trigger/{type}");
        Policies("pipelines:execute");
        Summary(s =>
        {
            s.Summary = "Trigger an orchestration node (proxy)";
            s.Description = "Forwards the trigger to the ETL server, which owns the orchestration queue.";
        });
        Tags("EtlOrchestration");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(TriggerOrchestrationNodeProxyRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "POST", "Etl", $"etl/trigger/{req.Type}");

        var client = _factory.CreateClient(EtlOrchestrationProxy.ClientName);
        using var response = await client
            .PostAsync($"etl/trigger/{req.Type}", content: null, ct)
            .ConfigureAwait(false);

        await SendUpstream(response, ct).ConfigureAwait(false);
    }

    private async Task SendUpstream(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            ProxyLog.ProxyFailed(_logger, "Etl", $"status {(int)response.StatusCode}");

        HttpContext.Response.StatusCode = (int)response.StatusCode;
        HttpContext.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        await HttpContext.Response.WriteAsync(body, ct).ConfigureAwait(false);
    }
}

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
        using var response = await client
            .GetAsync($"etl/executions/{req.Id}", ct)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            ProxyLog.ProxyFailed(_logger, "Etl", $"status {(int)response.StatusCode}");

        HttpContext.Response.StatusCode = (int)response.StatusCode;
        HttpContext.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        await HttpContext.Response.WriteAsync(body, ct).ConfigureAwait(false);
    }
}

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
        using var response = await client
            .PostAsync($"etl/executions/{req.Id}/approve", content: null, ct)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            ProxyLog.ProxyFailed(_logger, "Etl", $"status {(int)response.StatusCode}");

        HttpContext.Response.StatusCode = (int)response.StatusCode;
        HttpContext.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        await HttpContext.Response.WriteAsync(body, ct).ConfigureAwait(false);
    }
}
