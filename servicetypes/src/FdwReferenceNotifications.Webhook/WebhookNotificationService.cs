using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Logging;
using Fdw.Services.Notifications.Results;
using FdwReferenceNotifications.Webhook.Logging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceNotifications.Webhook;

/// <summary>
/// Notification service for sending webhook HTTP notifications.
/// Supports generic POST with a JSON payload including notification type, subject, body,
/// timestamp, and metadata. Optionally uses a custom payload template.
/// </summary>
public sealed class WebhookNotificationService : INotificationService
{
    /// <summary>The header that carries the secret: the one a pipeline's webhook trigger (FDW-1884) reads.</summary>
    public const string SecretHeader = "X-Webhook-Secret";

    // The pause before a delivery is tried again (FDW-1913): one fixed length, not configurable and not growing.
    private static readonly TimeSpan RetryPause = TimeSpan.FromSeconds(2);

    private readonly WebhookNotificationConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISecretManagerProvider _secretManagers;
    private readonly ILogger<WebhookNotificationService> _logger;
    private readonly string _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookNotificationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="secretManagers">Resolves the secret manager that holds the secret the service sends, when it names one.</param>
    /// <param name="configuration">The webhook configuration.</param>
    public WebhookNotificationService(
        ILogger<WebhookNotificationService> logger,
        IHttpClientFactory httpClientFactory,
        ISecretManagerProvider secretManagers,
        WebhookNotificationConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _secretManagers = secretManagers ?? throw new ArgumentNullException(nameof(secretManagers));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _id = Guid.NewGuid().ToString("N");
    }

    /// <inheritdoc/>
    public string Id => _id;

    /// <inheritdoc/>
    public string ServiceType => "Notification";

    /// <inheritdoc/>
    public string Name => _configuration.Name;

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

            if (SecretPartlyNamed() is { } missing)
            {
                return GenericResult<INotificationResult>.Failure(
                    WebhookNotificationServiceLogger.SecretPartlyNamed(_logger, _configuration.Name, missing));
            }

            if (_configuration.RetryCount < 0)
            {
                return GenericResult<INotificationResult>.Failure(
                    WebhookNotificationServiceLogger.RetryCountNegative(_logger, _configuration.Name, _configuration.RetryCount));
            }

            string? secret = null;
            if (!string.IsNullOrWhiteSpace(_configuration.SecretManagerName))
            {
                var read = await ReadSecret(cancellationToken).ConfigureAwait(false);
                if (read.IsFailure)
                {
                    return read.ToNewResult<INotificationResult>();
                }

                secret = read.Value;
            }

            using var httpClient = _httpClientFactory.CreateClient("WebhookNotifications");
            httpClient.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);

            var payload = BuildPayload(request);
            var tries = _configuration.RetryCount + 1;
            for (var attempt = 1; ; attempt++)
            {
                var delivery = await Deliver(httpClient, request, payload, secret, cancellationToken).ConfigureAwait(false);
                if (!delivery.CanRepeat || attempt >= tries)
                {
                    return delivery.Result;
                }

                WebhookNotificationServiceLogger.DeliveryRepeated(_logger, _configuration.Name, attempt, tries, delivery.Reason);
                await Task.Delay(RetryPause, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            return GenericResult<INotificationResult>.Failure(
                NotificationLogger.WebhookFailed(_logger, ex, ex.Message));
        }
    }

    // One try of the call. A receiver that cannot be reached, that does not answer in time, or that answers with a server error may answer
    // the same message next time; any other answer (a refusal included) would be the same again, so it ends the delivery (FDW-1913).
    private async Task<Delivery> Deliver(
        HttpClient httpClient, INotificationRequest request, string payload, string? secret, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(new HttpMethod(_configuration.Method), _configuration.Url)
        {
            Content = new StringContent(payload, Encoding.UTF8, _configuration.ContentType),
        };
        if (secret is not null)
        {
            httpRequest.Headers.Add(SecretHeader, secret);
        }

        try
        {
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var answered = await Answer(request, response, cancellationToken).ConfigureAwait(false);
            var status = (int)response.StatusCode;
            return new Delivery(answered, status >= 500, $"the receiver answered {status}");
        }
        catch (HttpRequestException ex)
        {
            return new Delivery(GenericResult<INotificationResult>.Failure(NotificationLogger.WebhookFailed(_logger, ex, ex.Message)), true, ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new Delivery(
                GenericResult<INotificationResult>.Failure(NotificationLogger.WebhookFailed(_logger, ex, ex.Message)),
                true,
                $"no answer within {_configuration.TimeoutSeconds} seconds");
        }
    }

    // What one try came to, and whether trying again could come to something else.
    private readonly record struct Delivery(IGenericResult<INotificationResult> Result, bool CanRepeat, string Reason);

    // What the receiver answered: a success status is a delivered notification, any other is a failed one carrying the status and body.
    private async Task<IGenericResult<INotificationResult>> Answer(
        INotificationRequest request, HttpResponseMessage response, CancellationToken cancellationToken)
    {
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

    // A service names its secret by the secret manager and the key, both or neither: neither sends no secret (its author chose none), and
    // one without the other is a service half set up, which sends nothing rather than call out unprotected by accident.
    private string? SecretPartlyNamed()
    {
        var manager = !string.IsNullOrWhiteSpace(_configuration.SecretManagerName);
        var key = !string.IsNullOrWhiteSpace(_configuration.SecretKeyName);
        return manager == key ? null : manager ? nameof(_configuration.SecretKeyName) : nameof(_configuration.SecretManagerName);
    }

    private async Task<IGenericResult<string>> ReadSecret(CancellationToken cancellationToken)
    {
        var managerName = _configuration.SecretManagerName!;
        var keyName = _configuration.SecretKeyName!;
        var manager = await _secretManagers.Get(managerName, cancellationToken).ConfigureAwait(false);
        if (!manager.IsSuccess || manager.Value is null)
        {
            return GenericResult<string>.Failure(
                WebhookNotificationServiceLogger.SecretUnreadable(_logger, _configuration.Name, managerName, keyName, manager.CurrentMessage));
        }

        var secret = await manager.Value
            .Execute(GetSecretManagerCommand.Latest(container: null, secretKey: keyName), cancellationToken)
            .ConfigureAwait(false);
        if (!secret.IsSuccess || secret.Value is null)
        {
            return GenericResult<string>.Failure(
                WebhookNotificationServiceLogger.SecretUnreadable(_logger, _configuration.Name, managerName, keyName, secret.CurrentMessage));
        }

        using var value = secret.Value;
        return value.GetStringValue() is { Length: > 0 } text
            ? GenericResult<string>.Success(text)
            : GenericResult<string>.Failure(
                WebhookNotificationServiceLogger.SecretUnreadable(_logger, _configuration.Name, managerName, keyName, "the secret is empty"));
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
