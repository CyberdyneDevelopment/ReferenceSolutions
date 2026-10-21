using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Replace a webhook trigger (proxy). Forwards the replacement to the ETL server.</summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateWebhookProxyEndpoint : WebhookProxyEndpointBase
{
    /// <inheritdoc />
    public UpdateWebhookProxyEndpoint(IHttpClientFactory factory, ILogger<UpdateWebhookProxyEndpoint> logger)
        : base(factory, logger)
    {
    }

    /// <inheritdoc />
    protected override string RequiredPolicy => "webhooks:write";

    /// <inheritdoc />
    protected override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Put;

    /// <inheritdoc />
    protected override string TargetPath() => $"webhooks/{Route<string>("Name")}";

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/webhooks/{Name}");
        ConfigureWebhooks("Replace a webhook trigger (proxy)", "Forwards the replacement to the ETL server.");
    }
}
