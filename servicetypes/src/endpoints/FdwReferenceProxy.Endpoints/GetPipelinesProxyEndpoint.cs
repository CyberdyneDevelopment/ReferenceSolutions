using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FdwReferenceProxy.Endpoints.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Proxy endpoint to list pipelines from the ETL server.
/// GET /api/v1/pipelines → EtlServer GET /api/v1/pipelines
/// </summary>
/// <remarks>
/// Pipelines had only their execution half proxied — TriggerPipelineProxy — while schedules had
/// their whole surface. Nothing else was missing: the ETL host already serves ListPipelinesEndpoint
/// and IPipelineClient already exposes List. The console's Pipelines screen 404ed for want of this
/// one file.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetPipelinesProxyEndpoint : EndpointWithoutRequest<IReadOnlyList<PipelineSummaryResponse>>
{
    private readonly IPipelineClient _client;
    private readonly ILogger<GetPipelinesProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetPipelinesProxyEndpoint"/> class.</summary>
    /// <param name="client">The client that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public GetPipelinesProxyEndpoint(IPipelineClient client, ILogger<GetPipelinesProxyEndpoint> logger)
    {
        _client = client;
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
            s.Description = "Proxies the request to the ETL server to list configured pipelines.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "GET", "Etl", "pipelines");

        var result = await _client.List(ct).ConfigureAwait(false);

        // 502, not an empty list: a proxy that cannot reach its upstream has not learned that there
        // are no pipelines. The console renders those two differently and cannot tell them apart
        // from a 200 with nothing in it.
        if (!result.IsSuccess)
        {
            ProxyLog.ProxyFailed(_logger, "Etl", result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            // A success with no value here is not expected on this path (see the client's own
            // non-success handling) -- kept as the prior fixed 502 rather than mapped, since
            // there is no real downstream status to relay for a call that itself succeeded.
            ProxyLog.ProxyFailed(_logger, "Etl", "Proxy request succeeded but returned no value");
            AddError("Failed to proxy request to EtlServer");
            await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Etl", 200);
        await Send.OkAsync(result.Value, ct).ConfigureAwait(false);
    }
}
