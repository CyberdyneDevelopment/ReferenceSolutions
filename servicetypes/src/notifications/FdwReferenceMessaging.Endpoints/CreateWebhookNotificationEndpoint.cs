using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using FdwReferenceNotifications.Webhook;
using Microsoft.Extensions.Logging;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>Makes a Webhook notification service, stating its address, method, content type, timeout, retries and secret names (FDW-1909).</summary>
[ExcludeFromCodeCoverage]
public class CreateWebhookNotificationEndpoint : CrudCreateEndpointBase<WebhookNotificationRequest, WebhookNotificationDetail>
{
    private readonly INotificationConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="CreateWebhookNotificationEndpoint"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored notification services.</param>
    public CreateWebhookNotificationEndpoint(ILogger<CreateWebhookNotificationEndpoint> logger, INotificationConfigurationProvider provider)
        : base(logger)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "notifications";

    /// <inheritdoc />
    protected override string Route => "/notifications/webhook";

    /// <inheritdoc />
    protected override string EndpointSummary => "Make a Webhook notification service";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Makes a notification service that calls a web address. State its address, method, content type, timeout, retry count and whether it is on; name the secret manager and key of the secret to send in X-Webhook-Secret, both or neither.";

    /// <inheritdoc />
    protected override string GetResourceName(WebhookNotificationRequest request) => request.Name;

    /// <inheritdoc />
    protected override string DuplicateMessage(WebhookNotificationRequest request) => $"A webhook notification service named '{request.Name}' already exists";

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(WebhookNotificationRequest request, CancellationToken ct)
    {
        var existing = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existing.IsSuccess && existing.Value != null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<WebhookNotificationDetail>> Create(WebhookNotificationRequest request, CancellationToken ct)
    {
        var record = new WebhookNotificationConfiguration();
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
