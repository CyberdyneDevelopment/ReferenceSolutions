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
/// Proxy endpoint to trigger a pipeline-type ETL job via the unified trigger route.
/// POST /etl/trigger/pipeline — matches ProjectApiClient.Trigger("pipeline", ...).
/// Translates TriggerRequest.Name → PipelineName for the ETL server.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TriggerPipelineProxyEndpoint : Endpoint<ProxyTriggerEtlRequest, TriggerPipelineResponse>
{
    private readonly IPipelineJobClient _client;
    private readonly ILogger<TriggerPipelineProxyEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerPipelineProxyEndpoint"/> class.
    /// </summary>
    public TriggerPipelineProxyEndpoint(IPipelineJobClient client, ILogger<TriggerPipelineProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/etl/trigger/pipeline");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:execute");
#endif
        Summary(s =>
        {
            s.Summary = "Trigger ETL pipeline (unified route)";
            s.Description = "Proxies the request to EtlServer to trigger a pipeline ETL execution. Called by ProjectApiClient.Trigger(\"pipeline\", ...).";
        });
        Tags("EtlOrchestration");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ProxyTriggerEtlRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "POST", "Etl", "etl/trigger/pipeline");

        var request = new TriggerPipelineRequest
        {
            Name = req.PipelineName,
            TriggerSource = req.TriggerSource,
            Parameters = req.Parameters,
            RunRoles = req.RunRoles,
        };

        var result = await _client.Trigger(request, ct);
        if (!result.IsSuccess)
        {
            // Why no message sniffing (FDW-899): ApiClientBase already turns the upstream HTTP status into a
            // ClientResultCodes code (400 -> UpstreamRequestInvalid, 404 -> UpstreamResourceNotFound), and
            // ResultHttpStatusMapper maps that code back. Matching "status 400" in the text turned every
            // refusal -- an unregistered TriggerSource included -- into a 404 "pipeline not found".
            ProxyLog.ProxyFailed(_logger, "Etl", result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Etl", 202);
        await Send.ResponseAsync(result.Value!, 202, ct);
    }
}
