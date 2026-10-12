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
/// Proxy endpoint to list pipeline engine types from the ETL server.
/// GET /api/v1/pipelines/types -> EtlServer GET /api/v1/pipelines/types
/// </summary>
/// <remarks>
/// Without this file, GET /pipelines/types fell through to GetPipelineDetailProxyEndpoint's
/// /pipelines/{Name} route and 400ed with "types" bound as a pipeline name. The pipeline wizard's
/// engine picker had no way to reach EtlPipelineTypes through the API host.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetPipelineTypesProxyEndpoint : EndpointWithoutRequest<IReadOnlyList<PipelineTypeSummary>>
{
    private readonly IPipelineClient _client;
    private readonly ILogger<GetPipelineTypesProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetPipelineTypesProxyEndpoint"/> class.</summary>
    /// <param name="client">The client that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public GetPipelineTypesProxyEndpoint(IPipelineClient client, ILogger<GetPipelineTypesProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/pipelines/types");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "List pipeline engine types (proxy)";
            s.Description = "Proxies the request to the ETL server to list registered pipeline engine types.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "GET", "Etl", "pipelines/types");

        var result = await _client.GetPipelineTypes(ct).ConfigureAwait(false);

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
