using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Webhooks;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to read a webhook trigger.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetWebhookEndpoint : GetWebhookEndpointBase
{
    /// <inheritdoc />
    public GetWebhookEndpoint(WebhookTriggerConfigurationProvider provider, PipelineServiceConfigurationProvider pipelines, IRoleGrants grants, ILogger<GetWebhookEndpoint> logger)
        : base(logger, provider, pipelines, grants)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Webhooks");
    }
}
