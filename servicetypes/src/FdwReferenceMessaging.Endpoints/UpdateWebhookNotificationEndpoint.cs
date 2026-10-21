using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using FdwReferenceNotifications.Webhook;
using Microsoft.Extensions.Logging;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>Changes a Webhook notification service by stating it whole again (FDW-1909).</summary>
/// <remarks>
/// Why the whole service and not the fields that changed: the secret names are part of what is stated, so leaving them out removes the secret
/// rather than keeping a secret the request did not mention.
/// </remarks>
[ExcludeFromCodeCoverage]
public class UpdateWebhookNotificationEndpoint : CrudUpdateEndpointBase<WebhookNotificationRequest, WebhookNotificationDetail>
{
    private readonly INotificationConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="UpdateWebhookNotificationEndpoint"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored notification services.</param>
    public UpdateWebhookNotificationEndpoint(ILogger<UpdateWebhookNotificationEndpoint> logger, INotificationConfigurationProvider provider)
        : base(logger)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "notifications";

    /// <inheritdoc />
    protected override string Route => "/notifications/webhook/{Name}";

    /// <inheritdoc />
    protected override HttpVerb UpdateVerb => HttpVerb.PUT;

    /// <inheritdoc />
    protected override string EndpointSummary => "Change a Webhook notification service";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Changes a Webhook notification service. State the whole service again; a secret manager and key left out remove the secret.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(WebhookNotificationRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<WebhookNotificationDetail?>> FindForUpdate(WebhookNotificationRequest request, CancellationToken ct)
    {
        var existing = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<WebhookNotificationDetail?>.Success(
            existing.IsSuccess && existing.Value is WebhookNotificationConfiguration record
                ? WebhookNotificationMapper.Detail(request.Name, record)
                : null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<WebhookNotificationDetail>> Update(
        WebhookNotificationRequest request, WebhookNotificationDetail existing, CancellationToken ct)
    {
        var current = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (!current.IsSuccess || current.Value is not WebhookNotificationConfiguration record)
        {
            return current.ToNewResult<WebhookNotificationDetail>();
        }

        WebhookNotificationMapper.Apply(request, record);
        var saved = await _provider
            .Save(record, WebhookNotificationMapper.Domain, WebhookNotificationMapper.Implementation, request.Name, ct)
            .ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<WebhookNotificationDetail>()
            : GenericResult<WebhookNotificationDetail>.Success(WebhookNotificationMapper.Detail(request.Name, record));
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
