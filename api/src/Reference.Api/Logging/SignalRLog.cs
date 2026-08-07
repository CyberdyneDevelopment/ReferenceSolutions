using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging for SignalR operations.
/// EventId range: 1650-1699
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class SignalRLog
{
    [MessageLogging(
        EventId = 1650,
        Level = LogLevel.Information,
        Message = "Client connected: {connectionId} (User: {userId})")]
    public static partial IGenericMessage ClientConnected(
        ILogger logger, string connectionId, string userId);

    [MessageLogging(
        EventId = 1651,
        Level = LogLevel.Information,
        Message = "Client disconnected: {connectionId}")]
    public static partial IGenericMessage ClientDisconnected(
        ILogger logger, string connectionId);

    [MessageLogging(
        EventId = 1652,
        Level = LogLevel.Warning,
        Message = "Client disconnected with error: {connectionId} - {error}")]
    public static partial IGenericMessage ClientDisconnectedWithError(
        ILogger logger, string connectionId, string error);

    [MessageLogging(
        EventId = 1653,
        Level = LogLevel.Debug,
        Message = "Client {connectionId} subscribed to calculation {calculationId}")]
    public static partial IGenericMessage SubscribedToCalculation(
        ILogger logger, string connectionId, string calculationId);

    [MessageLogging(
        EventId = 1654,
        Level = LogLevel.Debug,
        Message = "Notification '{notificationType}' sent for calculation {calculationId}")]
    public static partial IGenericMessage NotificationSent(
        ILogger logger, string notificationType, string calculationId);

    [MessageLogging(
        EventId = 1655,
        Level = LogLevel.Warning,
        Message = "Failed to send notification '{notificationType}': {error}")]
    public static partial IGenericMessage NotificationFailed(
        ILogger logger, string notificationType, string error);

    [MessageLogging(
        EventId = 1656,
        Level = LogLevel.Information,
        Message = "Active connections: {count}")]
    public static partial IGenericMessage ActiveConnections(
        ILogger logger, int count);

    [MessageLogging(
        EventId = 1657,
        Level = LogLevel.Debug,
        Message = "Client {connectionId} unsubscribed from calculation {calculationId}")]
    public static partial IGenericMessage UnsubscribedFromCalculation(
        ILogger logger, string connectionId, string calculationId);

    [MessageLogging(
        EventId = 1658,
        Level = LogLevel.Debug,
        Message = "Client {connectionId} subscribed to all calculations")]
    public static partial IGenericMessage SubscribedToAllCalculations(
        ILogger logger, string connectionId);
}
