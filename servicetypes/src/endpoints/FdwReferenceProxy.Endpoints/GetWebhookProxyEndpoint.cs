using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Get a webhook trigger (proxy). Forwards the read to the ETL server.</summary>
[ExcludeFromCodeCoverage]
public sealed class GetWebhookProxyEndpoint : WebhookProxyEndpointBase
{
    /// <inheritdoc />
    public GetWebhookProxyEndpoint(IHttpClientFactory factory, ILogger<GetWebhookProxyEndpoint> logger)
        : base(factory, logger)
    {
    }

    /// <inheritdoc />
    protected override string RequiredPolicy => "webhooks:read";

    /// <inheritdoc />
    protected override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Get;

    /// <inheritdoc />
    protected override string TargetPath() => $"webhooks/{Route<string>("Name")}";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/webhooks/{Name}");
        ConfigureWebhooks("Get a webhook trigger (proxy)", "Forwards the read to the ETL server.");
    }
}
