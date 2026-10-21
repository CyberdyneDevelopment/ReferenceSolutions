using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceNotifications.Webhook.Logging;

/// <summary>
/// Message logging for the secret a webhook notification service sends (FDW-1903). No message here carries a secret value: each
/// names the service, the secret manager and the key, which are names, never the secret.
/// </summary>
[MessageLoggingTypeCode("WEBHOOK")]
public static partial class WebhookNotificationServiceLogger
{
    /// <summary>Logs a service that names only one of the secret manager and the secret key.</summary>
    [MessageLogging(
        EventId = 21060,
        Level = LogLevel.Error,
        Message = "Webhook notification service '{serviceName}' names a secret but not its {missing}; it names both or neither, so it sends nothing")]
    public static partial IGenericMessage SecretPartlyNamed(
        ILogger<WebhookNotificationService> logger,
        string serviceName,
        string missing);

    /// <summary>Logs a secret that could not be read.</summary>
    [MessageLogging(
        EventId = 21061,
        Level = LogLevel.Error,
        Message = "Webhook notification service '{serviceName}' could not read its secret '{secretKey}' from secret manager '{secretManager}': {reason}")]
    public static partial IGenericMessage SecretUnreadable(
        ILogger<WebhookNotificationService> logger,
        string serviceName,
        string secretManager,
        string secretKey,
        string? reason);

    /// <summary>Logs a delivery that failed in a way worth trying again, before the next try.</summary>
    [MessageLogging(
        EventId = 21062,
        Level = LogLevel.Information,
        Message = "Webhook notification service '{serviceName}' try {attempt} of {tries} failed ({reason}); trying again")]
    public static partial IGenericMessage DeliveryRepeated(
        ILogger<WebhookNotificationService> logger,
        string serviceName,
        int attempt,
        int tries,
        string reason);

    /// <summary>Logs a service whose retry count is below zero.</summary>
    [MessageLogging(
        EventId = 21063,
        Level = LogLevel.Error,
        Message = "Webhook notification service '{serviceName}' has a retry count of {retryCount}, which must not be negative; it sends nothing")]
    public static partial IGenericMessage RetryCountNegative(
        ILogger<WebhookNotificationService> logger,
        string serviceName,
        int retryCount);
}
