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
/// Proxies the request to the ETL server to make a graph pipeline from a template.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateGraphFromTemplateProxyEndpoint : Endpoint<GraphFromTemplatePayload, GraphPayload>
{
    private readonly IPipelineClient _client;
    private readonly ILogger<CreateGraphFromTemplateProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="CreateGraphFromTemplateProxyEndpoint"/> class.</summary>
    /// <param name="client">The client that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public CreateGraphFromTemplateProxyEndpoint(IPipelineClient client, ILogger<CreateGraphFromTemplateProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/pipelines/from-template");
        Policies("pipelines:write");
        Summary(s =>
        {
            s.Summary = "Make a pipeline from a template (proxy)";
            s.Description = "Proxies the request to the ETL server to make a graph pipeline from a template.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(GraphFromTemplatePayload req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "POST", "Etl", "pipelines/from-template");

        var result = await _client.CreateGraphFromTemplate(req, ct).ConfigureAwait(false);

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

        ProxyLog.ProxyResponse(_logger, "Etl", 201);
        await Send.ResponseAsync(result.Value, 201, ct).ConfigureAwait(false);
    }
}
