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
/// Proxy endpoint to delete a pipeline on the ETL server.
/// DELETE /api/v1/pipelines/{name} -> EtlServer DELETE /api/v1/pipelines/{name}
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeletePipelineProxyEndpoint : Endpoint<PipelineNameProxyRequest>
{
    private readonly IPipelineClient _client;
    private readonly ILogger<DeletePipelineProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeletePipelineProxyEndpoint"/> class.</summary>
    /// <param name="client">The client that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public DeletePipelineProxyEndpoint(IPipelineClient client, ILogger<DeletePipelineProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Delete("/pipelines/{Name}");
        Policies("pipelines:delete");
        Summary(s =>
        {
            s.Summary = "Delete pipeline (proxy)";
            s.Description = "Proxies the request to the ETL server to delete a pipeline.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(PipelineNameProxyRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "DELETE", "Etl", $"pipelines/{req.Name}");

        var result = await _client.DeletePipeline(req.Name, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            ProxyLog.ProxyFailed(_logger, "Etl", result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Etl", 204);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
