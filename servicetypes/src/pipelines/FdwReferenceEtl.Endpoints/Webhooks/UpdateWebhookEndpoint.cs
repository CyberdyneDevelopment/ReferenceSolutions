using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Webhooks;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to replace a webhook trigger.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateWebhookEndpoint : UpdateWebhookEndpointBase
{
    /// <inheritdoc />
    public UpdateWebhookEndpoint(WebhookTriggerConfigurationProvider provider, PipelineServiceConfigurationProvider pipelines, IRoleGrants grants, ILogger<UpdateWebhookEndpoint> logger)
        : base(logger, provider, pipelines, grants)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Webhooks");
    }
}
