using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Webhooks;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to list the webhook triggers.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListWebhooksEndpoint : ListWebhooksEndpointBase
{
    /// <inheritdoc />
    public ListWebhooksEndpoint(WebhookTriggerConfigurationProvider provider, ILogger<ListWebhooksEndpoint> logger)
        : base(logger, provider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Webhooks");
    }
}
