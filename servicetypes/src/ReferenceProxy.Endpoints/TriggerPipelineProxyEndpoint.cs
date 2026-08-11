using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ReferenceProxy.Endpoints.Logging;

namespace ReferenceProxy.Endpoints;

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
            TriggerSource = req.TriggerSource
        };

        var result = await _client.Trigger(request, ct);
        if (!result.IsSuccess)
        {
            var errorMessage = result.CurrentMessage ?? "Proxy request failed";
            ProxyLog.ProxyFailed(_logger, "Etl", errorMessage);
            if (errorMessage.Contains("status 400", StringComparison.OrdinalIgnoreCase)
                || errorMessage.Contains("status 404", StringComparison.OrdinalIgnoreCase))
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            AddError("Failed to proxy request to EtlServer");
            await Send.ErrorsAsync(502, ct);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Etl", 202);
        await Send.ResponseAsync(result.Value!, 202, ct);
    }
}
