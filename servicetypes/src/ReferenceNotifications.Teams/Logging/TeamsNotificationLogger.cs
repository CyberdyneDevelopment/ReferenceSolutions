using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace ReferenceNotifications.Teams.Logging;

/// <summary>
/// Trace for the Teams notification channel. The generic notification events — an empty message,
/// no recipients, a failed validation — stay on FDW's NotificationLogger; what is here is only
/// what is true of Teams specifically.
/// </summary>
public static partial class TeamsNotificationLogger
{
    /// <summary>Logs when sending a Teams webhook notification.</summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Sending Teams notification to webhook")]
    public static partial IGenericMessage SendingTeamsNotification(ILogger logger);

    /// <summary>Logs when a Teams notification was sent successfully.</summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Debug,
        Message = "Teams notification sent successfully")]
    public static partial IGenericMessage TeamsSent(ILogger logger);

    /// <summary>Logs when the Teams webhook call itself failed.</summary>
    [MessageLogging(
        EventId = 71003,
        Level = LogLevel.Error,
        Message = "Teams webhook call failed: {error}")]
    public static partial IGenericMessage TeamsWebhookFailed(
        ILogger logger,
        Exception exception,
        string error);

    /// <summary>Logs when the Teams webhook answered with a non-success status.</summary>
    [MessageLogging(
        EventId = 71004,
        Level = LogLevel.Warning,
        Message = "Teams webhook returned status {statusCode}: {response}")]
    public static partial IGenericMessage TeamsWebhookNonSuccess(
        ILogger logger,
        int statusCode,
        string response);

    /// <summary>Logs when the Teams configuration is not usable.</summary>
    [MessageLogging(
        EventId = 61001,
        Level = LogLevel.Warning,
        Message = "Teams configuration is not valid: {reason}")]
    public static partial IGenericMessage InvalidTeamsConfiguration(
        ILogger logger,
        string reason);
}
