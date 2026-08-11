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
/// Proxy endpoint to trigger an ETL job on EtlServer.
/// POST /api/proxy/etl/trigger → EtlServer POST /api/v1/etl/trigger
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TriggerEtlJobProxyEndpoint : Endpoint<ProxyTriggerEtlRequest, TriggerPipelineResponse>
{
    private readonly IPipelineJobClient _client;
    private readonly ILogger<TriggerEtlJobProxyEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerEtlJobProxyEndpoint"/> class.
    /// </summary>
    public TriggerEtlJobProxyEndpoint(IPipelineJobClient client, ILogger<TriggerEtlJobProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/proxy/etl/trigger");
        Policies("pipelines:execute");
        Summary(s =>
        {
            s.Summary = "Trigger ETL job (proxy)";
            s.Description = "Proxies the request to EtlServer to trigger an ETL pipeline execution.";
            s.ExampleRequest = new ProxyTriggerEtlRequest
            {
                PipelineName = "DataArchiveCopy",
                TriggerSource = "API:ManualTrigger"
            };
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ProxyTriggerEtlRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "POST", "Etl", "etl/trigger");

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
            // Why: ETL server returns HTTP 400 when the requested pipeline is not found or not
            // triggerable (BatchCopy pipelines are not registered as ETL trigger targets). The
            // upstream client encodes the HTTP status in the message as "status 400". Surface
            // that as 404 to match REST convention (resource not found). Other ETL failures
            // (connection errors, 5xx) surface as 502 (bad gateway).
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
