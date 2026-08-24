using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Logging;
using Fdw.Services.Notifications.Results;
using Microsoft.Extensions.Logging;
using ReferenceNotifications.Webhook;
using Fdw.Services.Notifications;
using Fdw.Services;
using Fdw;

namespace ReferenceNotifications.Webhook;

/// <summary>
/// Notification service for sending webhook HTTP notifications.
/// Supports generic POST with a JSON payload including notification type, subject, body,
/// timestamp, and metadata. Optionally uses a custom payload template.
/// </summary>
public sealed class WebhookNotificationService : INotificationService
{
    private readonly WebhookNotificationConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookNotificationService> _logger;
    private readonly string _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookNotificationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="configuration">The webhook configuration.</param>
    public WebhookNotificationService(
        ILogger<WebhookNotificationService> logger,
        IHttpClientFactory httpClientFactory,
        WebhookNotificationConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _id = Guid.NewGuid().ToString("N");
    }

    /// <inheritdoc/>
    public string Id => _id;

    /// <inheritdoc/>
    public string Name => _configuration.Name;

    /// <inheritdoc/>
    public string ServiceType => "Webhook";

    /// <inheritdoc/>
    public bool IsAvailable => !string.IsNullOrEmpty(_configuration.Url);

    /// <inheritdoc/>
    public INotificationChannel Channel => NotificationChannels.ByName("Webhook");

    /// <inheritdoc/>
    public IGenericResult Validate(INotificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(_configuration.Url))
        {
            NotificationLogger.ValidationFailed(_logger, "Webhook URL is not configured");
            return GenericResult.Failure(
                NotificationResultCodes.ByName("NoWebhookUrl"));
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            NotificationLogger.EmptyMessage(_logger);
            return GenericResult.Failure(NotificationResultCodes.ByName("EmptyMessage"));
        }

        if (!Uri.TryCreate(_configuration.Url, UriKind.Absolute, out var uri) ||
            (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            NotificationLogger.ValidationFailed(_logger, $"Invalid webhook URL: {_configuration.Url}");
            return GenericResult.Failure(
                NotificationResultCodes.ByName("InvalidWebhookUrl"),
                ResultDetails.Create().With("WebhookUrl", _configuration.Url));
        }

        return GenericResult.Success();
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<INotificationResult>> Send(
        INotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            NotificationLogger.SendingWebhookNotification(_logger, _configuration.Url!);

            var payload = BuildPayload(request);
            var content = new StringContent(payload, Encoding.UTF8, _configuration.ContentType);

            using var httpClient = _httpClientFactory.CreateClient("WebhookNotifications");
            httpClient.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);

            var method = new HttpMethod(_configuration.Method);
            var httpRequest = new HttpRequestMessage(method, _configuration.Url)
            {
                Content = content
            };

            var response = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            NotificationLogger.WebhookSent(_logger, (int)response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                return GenericResult<INotificationResult>.Success(
                    NotificationResult.Success(request.RequestId, response.StatusCode.ToString()));
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return GenericResult<INotificationResult>.Success(
                NotificationResult.Failed(
                    request.RequestId,
                    $"Webhook returned {(int)response.StatusCode}: {responseBody}"));
        }
        catch (Exception ex)
        {
            return GenericResult<INotificationResult>.Failure(
                NotificationLogger.WebhookFailed(_logger, ex, ex.Message));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not INotificationRequest request)
        {
            return GenericResult<T>.Failure(
                NotificationResultCodes.ByName("UnsupportedCommand"),
                ResultDetails.Create().With("CommandType", command.GetType().Name));
        }

        var result = await Send(request, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return GenericResult<T>.Failure(
                NotificationResultCodes.ByName("SendFailed"),
                ResultDetails.Create().With("Message", result.CurrentMessage ?? "Unknown error"));
        }

        if (result.Value is T typedResult)
        {
            return GenericResult<T>.Success(typedResult);
        }

        return GenericResult<T>.Failure(
            NotificationResultCodes.ByName("UnexpectedResultType"),
            ResultDetails.Create().With("ExpectedType", typeof(T).Name));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not INotificationRequest request)
        {
            return GenericResult.Failure(
                NotificationResultCodes.ByName("UnsupportedCommand"),
                ResultDetails.Create().With("CommandType", command.GetType().Name));
        }

        var result = await Send(request, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? GenericResult.Success()
            : GenericResult.Failure(
                NotificationResultCodes.ByName("SendFailed"),
                ResultDetails.Create().With("Message", result.CurrentMessage ?? "Unknown error"));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // No unmanaged resources to dispose
    }

    private string BuildPayload(INotificationRequest request)
    {
        if (!string.IsNullOrEmpty(_configuration.PayloadTemplate))
        {
            return _configuration.PayloadTemplate
                .Replace("{subject}", request.Subject, StringComparison.Ordinal)
                .Replace("{body}", request.Message, StringComparison.Ordinal)
                .Replace("{type}", request.CommandType, StringComparison.Ordinal)
                .Replace("{timestamp}", request.CreatedAt.ToString("O"), StringComparison.Ordinal);
        }

        var metadata = request.Metadata != null
            ? new Dictionary<string, object?>(request.Metadata, StringComparer.Ordinal)
            : null;

        var payloadObject = new
        {
            type = request.CommandType,
            requestId = request.RequestId,
            subject = request.Subject,
            body = request.Message,
            priority = request.Priority.ToString(),
            timestamp = request.CreatedAt.ToString("O"),
            correlationId = request.CorrelationId,
            metadata
        };

        return JsonSerializer.Serialize(payloadObject);
    }
}
