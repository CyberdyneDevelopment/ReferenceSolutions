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
