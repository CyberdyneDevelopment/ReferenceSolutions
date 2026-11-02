using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Make a webhook trigger (proxy). Forwards the trigger to the ETL server, which owns webhook triggers. The answer carries the secret, once.</summary>
[ExcludeFromCodeCoverage]
public sealed class CreateWebhookProxyEndpoint : WebhookProxyEndpointBase
{
    /// <inheritdoc />
    public CreateWebhookProxyEndpoint(IHttpClientFactory factory, ILogger<CreateWebhookProxyEndpoint> logger)
        : base(factory, logger)
    {
    }

    /// <inheritdoc />
    protected override string RequiredPolicy => "webhooks:write";

    /// <inheritdoc />
    protected override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Post;

    /// <inheritdoc />
    protected override string TargetPath() => "webhooks";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/webhooks");
        ConfigureWebhooks("Make a webhook trigger (proxy)", "Forwards the trigger to the ETL server, which owns webhook triggers. The answer carries the secret, once.");
    }
}
