using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Scheduler.Server.Logging;

/// <summary>
/// MessageLogging for the outbound service-to-service client-credentials token flow that
/// authenticates the scheduler's pipeline-dispatch calls to the ETL server.
/// EventId range: 11160-11167.
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class OutboundTokenLog
{
    [MessageLogging(
        EventId = 11160,
        Level = LogLevel.Debug,
        Message = "Acquiring outbound client-credentials token for client '{clientId}' (scopes: {scopes})")]
    public static partial IGenericMessage TokenAcquisitionStarted(ILogger logger, string clientId, string scopes);

    [MessageLogging(
        EventId = 11161,
        Level = LogLevel.Error,
        Message = "Secret manager '{secretManagerName}' not found while resolving the outbound client secret '{secretKeyName}'")]
    public static partial IGenericMessage SecretManagerNotFound(ILogger logger, string secretManagerName, string secretKeyName);

    [MessageLogging(
        EventId = 11162,
        Level = LogLevel.Error,
        Message = "Failed to read outbound client secret '{secretKeyName}' from secret manager '{secretManagerName}': {reason}")]
    public static partial IGenericMessage SecretReadFailed(ILogger logger, string secretManagerName, string secretKeyName, string reason);

    [MessageLogging(
        EventId = 11163,
        Level = LogLevel.Error,
        Message = "Outbound client secret '{secretKeyName}' resolved to an empty or non-string value from secret manager '{secretManagerName}'")]
    public static partial IGenericMessage SecretMissing(ILogger logger, string secretManagerName, string secretKeyName);

    [MessageLogging(
        EventId = 11164,
        Level = LogLevel.Error,
        Message = "Failed to acquire outbound client-credentials token for client '{clientId}': {reason}")]
    public static partial IGenericMessage TokenAcquisitionFailed(ILogger logger, string clientId, string reason);

    [MessageLogging(
        EventId = 11165,
        Level = LogLevel.Error,
        Message = "Outbound client-credentials configuration is missing or incomplete (ClientId/SecretManagerName/SecretKeyName)")]
    public static partial IGenericMessage ClientCredentialsConfigurationMissing(ILogger logger);

    [MessageLogging(
        EventId = 11166,
        Level = LogLevel.Error,
        Message = "Unexpected error acquiring outbound client-credentials token for client '{clientId}'")]
    public static partial IGenericMessage TokenAcquisitionException(ILogger logger, Exception ex, string clientId);

    [MessageLogging(
        EventId = 11167,
        Level = LogLevel.Debug,
        Message = "Acquired outbound client-credentials token for client '{clientId}'")]
    public static partial IGenericMessage TokenAcquired(ILogger logger, string clientId);
}
