using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>List webhook triggers (proxy). Forwards the paged list (skip, take, total) to the ETL server.</summary>
[ExcludeFromCodeCoverage]
public sealed class ListWebhooksProxyEndpoint : WebhookProxyEndpointBase
{
    /// <inheritdoc />
    public ListWebhooksProxyEndpoint(IHttpClientFactory factory, ILogger<ListWebhooksProxyEndpoint> logger)
        : base(factory, logger)
    {
    }

    /// <inheritdoc />
    protected override string RequiredPolicy => "webhooks:read";

    /// <inheritdoc />
    protected override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Get;

    /// <inheritdoc />
    protected override string TargetPath() => "webhooks";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/webhooks");
        ConfigureWebhooks("List webhook triggers (proxy)", "Forwards the paged list (skip, take, total) to the ETL server.");
    }
}
