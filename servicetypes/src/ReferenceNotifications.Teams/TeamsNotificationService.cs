using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Logging;
using ReferenceNotifications.Teams.Logging;
using Fdw.Services.Notifications.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ReferenceNotifications.Teams;

/// <summary>
/// Notification service for sending messages to Microsoft Teams via webhook.
/// </summary>
/// <remarks>
/// Uses IOptionsMonitor for configuration to support hot-reload in singleton services.
/// Configuration changes are reflected immediately without service restart.
/// </remarks>
public sealed class TeamsNotificationService : INotificationService
{
    private readonly TeamsNotificationConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TeamsNotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamsNotificationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="configuration">The Teams target this service posts to.</param>
    public TeamsNotificationService(
        ILogger<TeamsNotificationService> logger,
        IHttpClientFactory httpClientFactory,
        TeamsNotificationConfiguration configuration)
    {
        // Why the configuration is handed in rather than watched through IOptionsMonitor: this
        // service is built per configured Teams target by the factory, the way every other channel
        // is. A monitor would bind one appsettings block for the whole process and make a second
        // target impossible.
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Gets the current Teams configuration.
    /// </summary>
    private TeamsNotificationConfiguration Configuration => _configuration;

    /// <inheritdoc/>
    public string Id => Configuration.Id.ToString();

    /// <inheritdoc />
    public string Name => _configuration.Name;

    /// <inheritdoc/>
    public string ServiceType => "Teams";

    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <inheritdoc/>
    public INotificationChannel Channel => NotificationChannels.ByName("Teams");

    /// <inheritdoc/>
    public IGenericResult Validate(INotificationRequest request)
    {
        if (request.Recipients.Count == 0 && string.IsNullOrEmpty(Configuration.DefaultWebhookUrl))
        {
            NotificationLogger.NoRecipients(_logger);
            return GenericResult.Failure(NotificationResultCodes.ByName("NoWebhookUrl"));
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            NotificationLogger.EmptyMessage(_logger);
            return GenericResult.Failure(NotificationResultCodes.ByName("EmptyMessage"));
        }

        // Validate webhook URLs
        foreach (var recipient in request.Recipients)
        {
            if (!Uri.TryCreate(recipient, UriKind.Absolute, out var uri) ||
                (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                NotificationLogger.ValidationFailed(_logger, $"Invalid webhook URL: {recipient}");
                return GenericResult.Failure(
                    NotificationResultCodes.ByName("InvalidWebhookUrl"),
                    ResultDetails.Create().With("WebhookUrl", recipient));
            }
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
            TeamsNotificationLogger.SendingTeamsNotification(_logger);

            var webhookUrls = request.Recipients.Count > 0
                ? request.Recipients
                : new[] { Configuration.DefaultWebhookUrl! };

            string? lastError = null;
            var allSucceeded = true;

            using var httpClient = _httpClientFactory.CreateClient("TeamsNotifications");
            httpClient.Timeout = TimeSpan.FromSeconds(Configuration.TimeoutSeconds);

            foreach (var webhookUrl in webhookUrls)
            {
                var payload = Configuration.UseAdaptiveCards
                    ? CreateAdaptiveCardPayload(request)
                    : CreateMessageCardPayload(request);

                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(webhookUrl, content, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    TeamsNotificationLogger.TeamsWebhookNonSuccess(_logger, (int)response.StatusCode, responseBody);
                    lastError = $"Teams webhook returned {response.StatusCode}: {responseBody}";
                    allSucceeded = false;
                }
            }

            if (allSucceeded)
            {
                TeamsNotificationLogger.TeamsSent(_logger);
                return GenericResult<INotificationResult>.Success(
                    NotificationResult.Success(request.RequestId));
            }
            else
            {
                return GenericResult<INotificationResult>.Success(
                    NotificationResult.Failed(request.RequestId, lastError ?? "Unknown error"));
            }
        }
        catch (Exception ex)
        {
            return GenericResult<INotificationResult>.Failure(
                TeamsNotificationLogger.TeamsWebhookFailed(_logger, ex, ex.Message));
        }
    }

    private static string CreateMessageCardPayload(INotificationRequest request)
    {
        var card = new
        {
            @type = "MessageCard",
            @context = "https://schema.org/extensions",
            summary = request.Subject,
            themeColor = GetThemeColor(request.Priority),
            title = request.Subject,
            text = request.Message
        };

        return JsonSerializer.Serialize(card);
    }

    private static string CreateAdaptiveCardPayload(INotificationRequest request)
    {
        var card = new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = new object[]
                        {
                            new
                            {
                                type = "TextBlock",
                                text = request.Subject,
                                weight = "Bolder",
                                size = "Large",
                                wrap = true
                            },
                            new
                            {
                                type = "TextBlock",
                                text = request.Message,
                                wrap = true
                            }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(card);
    }

    private static string GetThemeColor(INotificationPriority priority)
    {
        return priority.Name switch
        {
            "Critical" => "FF0000",
            "High" => "FFA500",
            "Normal" => "0078D4",
            "Low" => "808080",
            _ => "0078D4"
        };
    }

    /// <inheritdoc/>
    Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
    {
        if (command is INotificationRequest request)
        {
            return Task.FromResult(GenericResult<T>.Failure(NotificationResultCodes.ByName("UseSendMethod")));
        }
        return Task.FromResult(GenericResult<T>.Failure(
            NotificationResultCodes.ByName("UnsupportedCommand"),
            ResultDetails.Create().With("CommandType", command?.GetType().Name ?? "null")));
    }

    /// <inheritdoc/>
    Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
    {
        if (command is INotificationRequest request)
        {
            return Task.FromResult(GenericResult.Failure(NotificationResultCodes.ByName("UseSendMethod")));
        }
        return Task.FromResult(GenericResult.Failure(
            NotificationResultCodes.ByName("UnsupportedCommand"),
            ResultDetails.Create().With("CommandType", command?.GetType().Name ?? "null")));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // No unmanaged resources to dispose
    }
}
