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
/// Proxy endpoint to list the pipelines that are templates from the ETL server (FDW-3379).
/// GET /api/v1/pipelines/templates → EtlServer GET /api/v1/pipelines/templates
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListPipelineTemplatesProxyEndpoint : EndpointWithoutRequest<IReadOnlyList<PipelineSummaryResponse>>
{
    private readonly IPipelineClient _client;
    private readonly ILogger<ListPipelineTemplatesProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="ListPipelineTemplatesProxyEndpoint"/> class.</summary>
    /// <param name="client">The client that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public ListPipelineTemplatesProxyEndpoint(IPipelineClient client, ILogger<ListPipelineTemplatesProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/pipelines/templates");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "List pipeline templates (proxy)";
            s.Description = "Proxies the request to the ETL server to list the pipelines that are templates.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "GET", "Etl", "pipelines/templates");

        var result = await _client.ListTemplates(ct).ConfigureAwait(false);

        // 502, not an empty list: a proxy that cannot reach its upstream has not learned that there
        // are no templates. The console renders those two differently and cannot tell them apart
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
