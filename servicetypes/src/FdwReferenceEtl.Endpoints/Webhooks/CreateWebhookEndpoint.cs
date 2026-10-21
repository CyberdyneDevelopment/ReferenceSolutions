using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Webhooks;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to make a webhook trigger.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateWebhookEndpoint : CreateWebhookEndpointBase
{
    /// <inheritdoc />
    public CreateWebhookEndpoint(WebhookTriggerConfigurationProvider provider, PipelineServiceConfigurationProvider pipelines, IRoleGrants grants, ILogger<CreateWebhookEndpoint> logger)
        : base(logger, provider, pipelines, grants)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Webhooks");
    }
}
