using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;
using ReferenceNotifications.System;
using Fdw.Services.Notifications.System.Commands;
using Fdw.Services.Notifications.System;
using Fdw.Services.Notifications;
using Fdw.Services;
using Fdw;

namespace ReferenceNotifications.System.Logging;

/// <summary>
/// Static logger class for system notification operations using MessageLogging infrastructure.
/// Uses EventIds 7661-7680 (system notification operations).
/// </summary>
[MessageLoggingTypeCode("SYSTEM")]
public static partial class SystemNotificationLogger
{
    /// <summary>
    /// Logs when sending a system notification.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "[NOTIFICATION] [System] Sending notification via '{channel}': {subject}")]
    public static partial IGenericMessage SendingNotification(
        ILogger<SystemNotificationService> logger,
        string channel,
        string subject);

    /// <summary>
    /// Logs when a system notification has been sent successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "[NOTIFICATION] [System] Notification sent via '{channel}': {subject}")]
    public static partial IGenericMessage NotificationSent(
        ILogger<SystemNotificationService> logger,
        string channel,
        string subject);

    /// <summary>
    /// Logs when sending a system notification fails.
    /// </summary>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "[NOTIFICATION] [System] Failed to send notification via '{channel}': {errorMessage}")]
    public static partial IGenericMessage SendFailed(
        ILogger<SystemNotificationService> logger,
        string channel,
        string errorMessage);

    /// <summary>
    /// Logs when creating a system notification service.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Creating system notification service for '{configurationName}'")]
    public static partial IGenericMessage CreatingNotification(
        ILogger<SystemNotificationFactory> logger,
        string configurationName);

    /// <summary>
    /// Logs when a system notification service is created successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Created system notification service for '{configurationName}'")]
    public static partial IGenericMessage NotificationCreated(
        ILogger<SystemNotificationFactory> logger,
        string configurationName);

    /// <summary>
    /// Logs when factory receives null configuration.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "System notification factory received null configuration")]
    public static partial IGenericMessage ConfigurationNull(
        ILogger<SystemNotificationFactory> logger);

    /// <summary>
    /// Logs when factory receives invalid configuration type.
    /// </summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "Invalid configuration type. Expected SystemNotificationConfiguration, got '{actualType}'")]
    public static partial IGenericMessage InvalidConfigurationType(
        ILogger<SystemNotificationFactory> logger,
        string actualType);

    /// <summary>
    /// Logs when factory fails to create a notification service.
    /// </summary>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "Failed to create system notification service for '{configurationName}': {errorMessage}")]
    public static partial IGenericMessage CreationFailed(
        ILogger<SystemNotificationFactory> logger,
        string configurationName,
        string errorMessage);

    /// <summary>
    /// Logs when created notification is not of the expected type.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Created notification is not of expected type '{expectedType}'")]
    public static partial IGenericMessage UnexpectedNotificationType(
        ILogger<SystemNotificationFactory> logger,
        string expectedType);
}
