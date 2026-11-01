using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Webhooks;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to make a new secret for a webhook trigger.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class NewWebhookSecretEndpoint : NewWebhookSecretEndpointBase
{
    /// <inheritdoc />
    public NewWebhookSecretEndpoint(WebhookTriggerConfigurationProvider provider, PipelineServiceConfigurationProvider pipelines, IRoleGrants grants, ILogger<NewWebhookSecretEndpoint> logger)
        : base(logger, provider, pipelines, grants)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Webhooks");
    }
}
