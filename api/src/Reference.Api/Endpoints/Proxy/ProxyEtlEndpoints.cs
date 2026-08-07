using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Reference.Api.Constants;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints.Proxy;

// ═══════════════════════════════════════════════════════════════════════════
// Proxy ETL Request/Response DTOs
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Request for triggering an ETL job via proxy.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ProxyTriggerEtlRequest
{
    /// <summary>
    /// Gets or sets the pipeline name to execute.
    /// </summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the trigger source identifier.
    /// </summary>
    public string? TriggerSource { get; set; }
}

/// <summary>
/// Request for receiving ETL webhook completion callbacks.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EtlWebhookRequest
{
    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the completion timestamp.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>
/// Response for ETL webhook acknowledgement.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EtlWebhookResponse
{
    /// <summary>
    /// Gets or sets whether the webhook was acknowledged.
    /// </summary>
    public bool Acknowledged { get; set; }

    /// <summary>
    /// Gets or sets the execution ID that was processed.
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;
}

// ═══════════════════════════════════════════════════════════════════════════
// Proxy ETL Endpoints
// ═══════════════════════════════════════════════════════════════════════════

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
            AddError(Constants.ErrorMessages.FailedToProxyEtl);
            await Send.ErrorsAsync(502, ct);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Etl", 202);
        await Send.ResponseAsync(result.Value!, 202, ct);
    }
}

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
            AddError(Constants.ErrorMessages.FailedToProxyEtl);
            await Send.ErrorsAsync(502, ct);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Etl", 202);
        await Send.ResponseAsync(result.Value!, 202, ct);
    }
}

/// <summary>
/// Endpoint to receive ETL webhook completion callbacks from EtlServer.
/// POST /api/proxy/etl/webhook/completion
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EtlWebhookEndpoint : Endpoint<EtlWebhookRequest, EtlWebhookResponse>
{
    private readonly ILogger<EtlWebhookEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtlWebhookEndpoint"/> class.
    /// </summary>
    public EtlWebhookEndpoint(ILogger<EtlWebhookEndpoint> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/proxy/etl/webhook/completion");
        Policies("pipelines:execute");
        Summary(s =>
        {
            s.Summary = "ETL webhook completion callback";
            s.Description = "Receives completion callbacks from EtlServer when ETL pipeline executions finish.";
        });
        Tags("Pipelines");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(EtlWebhookRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.ExecutionId))
        {
            ProxyLog.EtlWebhookUnknownExecution(_logger, req.ExecutionId);
            await Send.ResponseAsync(new EtlWebhookResponse
            {
                Acknowledged = false,
                ExecutionId = req.ExecutionId
            }, 400, ct);
            return;
        }

        ProxyLog.EtlWebhookReceived(_logger, req.ExecutionId, req.Status);

        // Webhook acknowledged. Downstream processing (execution tracking updates,
        // SignalR client notifications) is handled by the Operations module when configured.

        await Send.OkAsync(new EtlWebhookResponse
        {
            Acknowledged = true,
            ExecutionId = req.ExecutionId
        }, ct);
    }
}
