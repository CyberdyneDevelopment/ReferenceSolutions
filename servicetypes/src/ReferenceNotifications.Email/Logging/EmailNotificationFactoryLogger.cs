using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;
using ReferenceNotifications.Email;
using Fdw.Services.Notifications;
using Fdw.Services;
using Fdw;

namespace ReferenceNotifications.Email.Logging;

/// <summary>
/// Static logger class for Email notification factory operations using MessageLogging infrastructure.
/// Uses EventIds 7601-7620 (email factory operations).
/// </summary>
[MessageLoggingTypeCode("EMAIL")]
public static partial class EmailNotificationFactoryLogger
{
    /// <summary>
    /// Logs when creating an email notification service.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Creating email notification service for '{configurationName}'")]
    public static partial IGenericMessage CreatingNotification(
        ILogger<EmailNotificationFactory> logger,
        string configurationName);

    /// <summary>
    /// Logs when an email notification service is created successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Created email notification service for '{configurationName}' using SMTP host '{smtpHost}'")]
    public static partial IGenericMessage NotificationCreated(
        ILogger<EmailNotificationFactory> logger,
        string configurationName,
        string smtpHost);

    /// <summary>
    /// Logs when factory receives null configuration.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Error,
        Message = "Email factory received null configuration")]
    public static partial IGenericMessage ConfigurationNull(
        ILogger<EmailNotificationFactory> logger);

    /// <summary>
    /// Logs when factory receives invalid configuration type.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "Invalid configuration type. Expected EmailConfiguration, got '{actualType}'")]
    public static partial IGenericMessage InvalidConfigurationType(
        ILogger<EmailNotificationFactory> logger,
        string actualType);

    /// <summary>
    /// Logs when factory fails to create a notification service.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Failed to create email notification service for '{configurationName}': {errorMessage}")]
    public static partial IGenericMessage CreationFailed(
        ILogger<EmailNotificationFactory> logger,
        string configurationName,
        string errorMessage);

    /// <summary>
    /// Logs when SMTP configuration is invalid.
    /// </summary>
    [MessageLogging(
        EventId = 61001,
        Level = LogLevel.Error,
        Message = "Invalid SMTP configuration for '{configurationName}': {reason}")]
    public static partial IGenericMessage InvalidSmtpConfiguration(
        ILogger<EmailNotificationFactory> logger,
        string configurationName,
        string reason);

    /// <summary>
    /// Logs when created notification is not of the expected type.
    /// </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Created notification is not of expected type '{expectedType}'")]
    public static partial IGenericMessage UnexpectedNotificationType(
        ILogger<EmailNotificationFactory> logger,
        string expectedType);
}
