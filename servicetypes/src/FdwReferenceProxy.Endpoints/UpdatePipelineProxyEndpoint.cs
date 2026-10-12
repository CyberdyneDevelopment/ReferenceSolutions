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
/// Proxy endpoint to update a pipeline on the ETL server.
/// PATCH /api/v1/pipelines/{name} -> EtlServer PATCH /api/v1/pipelines/{name}
/// </summary>
/// <remarks>
/// The request is the client request itself, not a type of this endpoint's own: a copy of its members is dropped member by
/// member the day the client request gains one, and the update then reaches the ETL server without it (FDW-1154, reject
/// steps). The pipeline's name is the route's, read here, not a member of the body.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class UpdatePipelineProxyEndpoint : Endpoint<UpdatePipelineClientRequest, PipelineDetailResponse>
{
    private readonly IPipelineClient _client;
    private readonly ILogger<UpdatePipelineProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="UpdatePipelineProxyEndpoint"/> class.</summary>
    /// <param name="client">The client that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public UpdatePipelineProxyEndpoint(IPipelineClient client, ILogger<UpdatePipelineProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        // Why PATCH and not PUT: an update states only what it changes, which is what the
        // nullable request properties above express -- same reasoning as UpdateScheduleProxyEndpoint.
        Patch("/pipelines/{Name}");
        Policies("pipelines:write");
        Summary(s =>
        {
            s.Summary = "Update pipeline (proxy)";
            s.Description = "Proxies the request to the ETL server to update an existing pipeline.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(UpdatePipelineClientRequest req, CancellationToken ct)
    {
        var name = Route<string>("Name")!;
        ProxyLog.ProxyRequest(_logger, "PATCH", "Etl", $"pipelines/{name}");

        var result = await _client.UpdatePipeline(name, req, ct).ConfigureAwait(false);

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
