using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Delete a webhook trigger (proxy). Forwards the delete to the ETL server.</summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteWebhookProxyEndpoint : WebhookProxyEndpointBase
{
    /// <inheritdoc />
    public DeleteWebhookProxyEndpoint(IHttpClientFactory factory, ILogger<DeleteWebhookProxyEndpoint> logger)
        : base(factory, logger)
    {
    }

    /// <inheritdoc />
    protected override string RequiredPolicy => "webhooks:delete";

    /// <inheritdoc />
    protected override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Delete;

    /// <inheritdoc />
    protected override string TargetPath() => $"webhooks/{Route<string>("Name")}";

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/webhooks/{Name}");
        ConfigureWebhooks("Delete a webhook trigger (proxy)", "Forwards the delete to the ETL server.");
    }
}
