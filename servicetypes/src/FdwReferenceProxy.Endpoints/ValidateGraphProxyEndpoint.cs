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
/// Proxies the request to the ETL server to check a graph pipeline draft without saving it.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ValidateGraphProxyEndpoint : Endpoint<GraphPayload, GraphValidationPayload>
{
    private readonly IPipelineClient _client;
    private readonly ILogger<ValidateGraphProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="ValidateGraphProxyEndpoint"/> class.</summary>
    /// <param name="client">The client that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public ValidateGraphProxyEndpoint(IPipelineClient client, ILogger<ValidateGraphProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/pipelines/graph/validate");
        Policies("pipelines:write");
        Summary(s =>
        {
            s.Summary = "Check graph pipeline draft (proxy)";
            s.Description = "Proxies the request to the ETL server to check a graph pipeline draft without saving it.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(GraphPayload req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "POST", "Etl", "pipelines/graph/validate");

        var result = await _client.ValidateGraph(req, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            ProxyLog.ProxyFailed(_logger, "Etl", result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            ProxyLog.ProxyFailed(_logger, "Etl", "Proxy request succeeded but returned no value");
            AddError("Failed to proxy request to EtlServer");
            await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Etl", 200);
        await Send.OkAsync(result.Value, ct).ConfigureAwait(false);
    }
}
