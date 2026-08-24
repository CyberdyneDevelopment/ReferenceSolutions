using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;
using ReferenceNotifications.Console;
using Fdw.Services.Notifications;
using Fdw.Services;
using Fdw;

namespace ReferenceNotifications.Console.Logging;

/// <summary>
/// Static logger class for Console notification operations using MessageLogging infrastructure.
/// Uses EventIds 7641-7660 (console notification operations).
/// </summary>
[MessageLoggingTypeCode("CONSOLE")]
public static partial class ConsoleNotificationLogger
{
    /// <summary>
    /// Logs notification content at Information level (dev/test channel output).
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "[NOTIFICATION] [{channel}] Subject: {subject} | Body: {body}")]
    public static partial IGenericMessage NotificationLogged(
        ILogger<ConsoleNotificationService> logger,
        string channel,
        string subject,
        string body);

    /// <summary>
    /// Logs when creating a console notification service.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Creating console notification service for '{configurationName}'")]
    public static partial IGenericMessage CreatingNotification(
        ILogger<ConsoleNotificationFactory> logger,
        string configurationName);

    /// <summary>
    /// Logs when a console notification service is created successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Created console notification service for '{configurationName}'")]
    public static partial IGenericMessage NotificationCreated(
        ILogger<ConsoleNotificationFactory> logger,
        string configurationName);

    /// <summary>
    /// Logs when factory receives null configuration.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "Console notification factory received null configuration")]
    public static partial IGenericMessage ConfigurationNull(
        ILogger<ConsoleNotificationFactory> logger);

    /// <summary>
    /// Logs when factory receives invalid configuration type.
    /// </summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "Invalid configuration type. Expected ConsoleNotificationConfiguration, got '{actualType}'")]
    public static partial IGenericMessage InvalidConfigurationType(
        ILogger<ConsoleNotificationFactory> logger,
        string actualType);

    /// <summary>
    /// Logs when factory fails to create a notification service.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Failed to create console notification service for '{configurationName}': {errorMessage}")]
    public static partial IGenericMessage CreationFailed(
        ILogger<ConsoleNotificationFactory> logger,
        string configurationName,
        string errorMessage);

    /// <summary>
    /// Logs when created notification is not of the expected type.
    /// </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Created notification is not of expected type '{expectedType}'")]
    public static partial IGenericMessage UnexpectedNotificationType(
        ILogger<ConsoleNotificationFactory> logger,
        string expectedType);
}
