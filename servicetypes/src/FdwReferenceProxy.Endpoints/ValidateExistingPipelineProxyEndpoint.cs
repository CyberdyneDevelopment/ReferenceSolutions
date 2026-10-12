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
/// Proxy endpoint that checks an edit of a saved pipeline without saving it.
/// POST /api/v1/pipelines/{name}/validate -> EtlServer POST /api/v1/pipelines/{name}/validate
/// </summary>
/// <remarks>
/// Why a proxy: the console talks to the API host and the ETL server owns the pipeline checks. The body is the update's,
/// so the edit is judged by exactly the checks updating the pipeline with it would run, run roles included (FDW-1105).
/// Why the request is the client request itself and not a type of this endpoint's own: a copy of its members is dropped
/// member by member the day the client request gains one, and the edit then reaches the ETL server without it (FDW-1154,
/// reject steps). The pipeline's name is the route's, read here, not a member of the body.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ValidateExistingPipelineProxyEndpoint : Endpoint<UpdatePipelineClientRequest, ValidatePipelineResponse>
{
    private readonly IPipelineClient _client;
    private readonly ILogger<ValidateExistingPipelineProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="ValidateExistingPipelineProxyEndpoint"/> class.</summary>
    /// <param name="client">The client that reaches the ETL server.</param>
    /// <param name="logger">The logger for this endpoint.</param>
    public ValidateExistingPipelineProxyEndpoint(IPipelineClient client, ILogger<ValidateExistingPipelineProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/pipelines/{Name}/validate");
        Policies("pipelines:write");
        Summary(s =>
        {
            s.Summary = "Check an edit of a saved pipeline without saving it (proxy)";
            s.Description = "Proxies the edit to the ETL server, which answers every problem updating the pipeline with it would be refused for. An empty list means the update would accept it. Answers 404 when no pipeline has the name.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(UpdatePipelineClientRequest req, CancellationToken ct)
    {
        var name = Route<string>("Name")!;
        ProxyLog.ProxyRequest(_logger, "POST", "Etl", $"pipelines/{name}/validate");

        var result = await _client.ValidatePipeline(name, req, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            ProxyLog.ProxyFailed(_logger, "Etl", result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            // A success with no value here is not expected on this path; kept as the fixed 502 the other pipeline
            // proxies answer, since there is no downstream status to relay for a call that succeeded.
            ProxyLog.ProxyFailed(_logger, "Etl", "Proxy request succeeded but returned no value");
            AddError("Failed to proxy request to EtlServer");
            await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Etl", 200);
        await Send.OkAsync(result.Value, ct).ConfigureAwait(false);
    }
}
