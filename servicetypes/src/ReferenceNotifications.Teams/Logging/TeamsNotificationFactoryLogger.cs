using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace ReferenceNotifications.Teams.Logging;

/// <summary>Trace for construction of the Teams notification service.</summary>
public static partial class TeamsNotificationFactoryLogger
{
    /// <summary>Logs when no configuration was supplied.</summary>
    [MessageLogging(
        EventId = 61010,
        Level = LogLevel.Error,
        Message = "Teams notification configuration was null")]
    public static partial IGenericMessage ConfigurationNull(ILogger logger);

    /// <summary>Logs when the configuration names no webhook to post to.</summary>
    [MessageLogging(
        EventId = 61011,
        Level = LogLevel.Error,
        Message = "Teams notification '{name}' declares no DefaultWebhookUrl")]
    public static partial IGenericMessage WebhookUrlMissing(ILogger logger, string name);

    /// <summary>Logs when the service is being constructed.</summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Debug,
        Message = "Creating Teams notification service '{name}'")]
    public static partial IGenericMessage CreatingNotification(ILogger logger, string name);

    /// <summary>Logs when the service was constructed.</summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "Teams notification service '{name}' created")]
    public static partial IGenericMessage NotificationCreated(ILogger logger, string name);

    /// <summary>Logs when construction threw.</summary>
    [MessageLogging(
        EventId = 71010,
        Level = LogLevel.Error,
        Message = "Creating Teams notification service '{name}' failed: {error}")]
    public static partial IGenericMessage CreateFailed(ILogger logger, Exception exception, string name, string error);

    /// <summary>Logs when the configuration handed over was for a different channel.</summary>
    [MessageLogging(
        EventId = 61012,
        Level = LogLevel.Error,
        Message = "Teams notification factory was given a {actual}, which is not a Teams configuration")]
    public static partial IGenericMessage InvalidConfigurationType(ILogger logger, string actual);

    /// <summary>Logs when the constructed service is not the type the caller asked for.</summary>
    [MessageLogging(
        EventId = 61013,
        Level = LogLevel.Error,
        Message = "Teams notification service is not a {requested}")]
    public static partial IGenericMessage UnexpectedNotificationType(ILogger logger, string requested);
}
