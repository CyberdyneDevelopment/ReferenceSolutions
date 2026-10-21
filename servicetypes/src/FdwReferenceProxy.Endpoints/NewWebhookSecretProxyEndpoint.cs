using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Make a new secret for a webhook trigger (proxy). Forwards the request to the ETL server. The answer carries the new secret, once.</summary>
[ExcludeFromCodeCoverage]
public sealed class NewWebhookSecretProxyEndpoint : WebhookProxyEndpointBase
{
    /// <inheritdoc />
    public NewWebhookSecretProxyEndpoint(IHttpClientFactory factory, ILogger<NewWebhookSecretProxyEndpoint> logger)
        : base(factory, logger)
    {
    }

    /// <inheritdoc />
    protected override string RequiredPolicy => "webhooks:write";

    /// <inheritdoc />
    protected override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Post;

    /// <inheritdoc />
    protected override string TargetPath() => $"webhooks/{Route<string>("Name")}/secret";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/webhooks/{Name}/secret");
        ConfigureWebhooks("Make a new secret for a webhook trigger (proxy)", "Forwards the request to the ETL server. The answer carries the new secret, once.");
    }
}
