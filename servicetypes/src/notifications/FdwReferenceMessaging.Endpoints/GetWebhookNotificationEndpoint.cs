using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Endpoints;
using Fdw.Web.RestEndpoints.Crud;
using FdwReferenceNotifications.Webhook;
using Microsoft.Extensions.Logging;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>Reads a Webhook notification service back, with the names of its secret and never the secret (FDW-1909).</summary>
[ExcludeFromCodeCoverage]
public class GetWebhookNotificationEndpoint : CrudGetEndpointBase<NotificationNameRequest, WebhookNotificationDetail>
{
    private readonly INotificationConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="GetWebhookNotificationEndpoint"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored notification services.</param>
    public GetWebhookNotificationEndpoint(ILogger<GetWebhookNotificationEndpoint> logger, INotificationConfigurationProvider provider)
        : base(logger)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "notifications";

    /// <inheritdoc />
    protected override string Route => "/notifications/webhook/{Name}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get a Webhook notification service";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Returns a Webhook notification service: its address, method, content type, timeout, retry count and the names of its secret manager and key. It never returns a secret.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(NotificationNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<WebhookNotificationDetail?>> FindByIdentifier(NotificationNameRequest request, CancellationToken ct)
    {
        var existing = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<WebhookNotificationDetail?>.Success(
            existing.IsSuccess && existing.Value is WebhookNotificationConfiguration record
                ? WebhookNotificationMapper.Detail(request.Name, record)
                : null);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
