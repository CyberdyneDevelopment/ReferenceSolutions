using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;
using ReferenceNotifications.Webhook;
using Fdw.Services.Notifications.Webhook.Commands;
using Fdw.Services.Notifications.Webhook;
using Fdw.Services.Notifications;
using Fdw.Services;
using Fdw;

namespace ReferenceNotifications.Webhook.Logging;

/// <summary>
/// Static logger class for Webhook notification factory operations using MessageLogging infrastructure.
/// Uses EventIds 7621-7640 (webhook factory operations).
/// </summary>
[MessageLoggingTypeCode("WEBHOOK")]
public static partial class WebhookNotificationFactoryLogger
{
    /// <summary>
    /// Logs when creating a webhook notification service.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Creating webhook notification service for '{configurationName}'")]
    public static partial IGenericMessage CreatingNotification(
        ILogger<WebhookNotificationFactory> logger,
        string configurationName);

    /// <summary>
    /// Logs when a webhook notification service is created successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Created webhook notification service for '{configurationName}' targeting '{url}'")]
    public static partial IGenericMessage NotificationCreated(
        ILogger<WebhookNotificationFactory> logger,
        string configurationName,
        string url);

    /// <summary>
    /// Logs when factory receives null configuration.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "Webhook factory received null configuration")]
    public static partial IGenericMessage ConfigurationNull(
        ILogger<WebhookNotificationFactory> logger);

    /// <summary>
    /// Logs when factory receives invalid configuration type.
    /// </summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "Invalid configuration type. Expected WebhookNotificationConfiguration, got '{actualType}'")]
    public static partial IGenericMessage InvalidConfigurationType(
        ILogger<WebhookNotificationFactory> logger,
        string actualType);

    /// <summary>
    /// Logs when factory fails to create a notification service.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Failed to create webhook notification service for '{configurationName}': {errorMessage}")]
    public static partial IGenericMessage CreationFailed(
        ILogger<WebhookNotificationFactory> logger,
        string configurationName,
        string errorMessage);

    /// <summary>
    /// Logs when webhook URL is missing from configuration.
    /// </summary>
    [MessageLogging(
        EventId = 21002,
        Level = LogLevel.Error,
        Message = "Webhook URL is required but not configured for '{configurationName}'")]
    public static partial IGenericMessage WebhookUrlMissing(
        ILogger<WebhookNotificationFactory> logger,
        string configurationName);

    /// <summary>
    /// Logs when created notification is not of the expected type.
    /// </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Created notification is not of expected type '{expectedType}'")]
    public static partial IGenericMessage UnexpectedNotificationType(
        ILogger<WebhookNotificationFactory> logger,
        string expectedType);
}
