using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;
using Fdw.Services.Credentials;
using Fdw.Services.Credentials.Logging;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.Credentials.Sql.Options;
using Fdw.Services.Credentials.Sql.Outcomes;
using Fdw.Services.DataVault;
using Fdw.Services.DataVault.Logging;
using Fdw.Services;
using Fdw;

namespace ReferenceCredentials.Sql.Logging;

/// <summary>
/// MessageLogging for SQL-backed Personal Access Token operations.
/// EventId range: 7940-7959. Token values are never logged.
/// </summary>
[MessageLoggingTypeCode("SQL")]
public static partial class PersonalAccessTokenLog
{
    /// <summary>Logged at Information level when a personal access token is created.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information, Message = "Personal access token created for user '{userId}' (token '{tokenId}')")]
    public static partial IGenericMessage TokenCreated(ILogger logger, Guid userId, Guid tokenId);

    /// <summary>Logged at Warning level when the per-user active token limit is reached.</summary>
    [MessageLogging(EventId = 41001, Level = LogLevel.Warning, Message = "Personal access token limit ({maxTokens}) reached for user '{userId}'")]
    public static partial IGenericMessage TokenLimitReached(ILogger logger, Guid userId, int maxTokens);

    /// <summary>Logged at Information level when a personal access token is revoked.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information, Message = "Personal access token '{tokenId}' revoked for user '{userId}'")]
    public static partial IGenericMessage TokenRevoked(ILogger logger, Guid userId, Guid tokenId);

    /// <summary>Logged at Warning level when a personal access token validation fails.</summary>
    [MessageLogging(EventId = 51000, Level = LogLevel.Warning, Message = "Personal access token validation failed")]
    public static partial IGenericMessage ValidationFailed(ILogger logger);

    /// <summary>Logged at Error level when a database error occurs during a token operation.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error, Message = "Database error during personal access token operation '{operation}'")]
    public static partial IGenericMessage DatabaseError(ILogger logger, Exception ex, string operation);

    /// <summary>Logged at Warning level when a token is not found for the caller.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning, Message = "Personal access token '{tokenId}' not found for user '{userId}'")]
    public static partial IGenericMessage TokenNotFound(ILogger logger, Guid userId, Guid tokenId);

    /// <summary>Logged at Error level when the vault name is not configured.</summary>
    [MessageLogging(EventId = 61002, Level = LogLevel.Error, Message = "Personal access token credential vault name is missing from configuration")]
    public static partial IGenericMessage VaultNameMissing(ILogger logger);

    /// <summary>Logged at Error level when the vault cannot be resolved by name.</summary>
    [MessageLogging(EventId = 61003, Level = LogLevel.Error, Message = "Personal access token credential vault '{vaultName}' could not be resolved")]
    public static partial IGenericMessage VaultResolveFailed(ILogger logger, string vaultName);

    /// <summary>Logged at Error level when the HMAC secret manager name is not configured.</summary>
    [MessageLogging(EventId = 61004, Level = LogLevel.Error, Message = "Personal access token HMAC secret manager name is missing from configuration")]
    public static partial IGenericMessage HmacSecretManagerNameMissing(ILogger logger);

    /// <summary>Logged at Error level when the HMAC secret key name is not configured.</summary>
    [MessageLogging(EventId = 61005, Level = LogLevel.Error, Message = "Personal access token HMAC secret key name is missing from configuration")]
    public static partial IGenericMessage HmacSecretKeyNameMissing(ILogger logger);

    /// <summary>Logged at Error level when the HMAC secret manager cannot be resolved.</summary>
    [MessageLogging(EventId = 61006, Level = LogLevel.Error, Message = "Personal access token HMAC secret manager '{secretManagerName}' could not be resolved")]
    public static partial IGenericMessage HmacSecretManagerResolveFailed(ILogger logger, string secretManagerName);

    /// <summary>Logged at Error level when the HMAC key secret cannot be read.</summary>
    [MessageLogging(EventId = 61007, Level = LogLevel.Error, Message = "Personal access token HMAC key secret '{secretKeyName}' could not be read from '{secretManagerName}'")]
    public static partial IGenericMessage HmacKeySecretReadFailed(ILogger logger, string secretManagerName, string secretKeyName);

    /// <summary>Logged at Error level when the token environment segment is not configured.</summary>
    [MessageLogging(EventId = 61008, Level = LogLevel.Error, Message = "Personal access token environment segment is missing from the credential service configuration")]
    public static partial IGenericMessage EnvironmentMissing(ILogger logger);

    /// <summary>Logged at Error level when the per-user token limit is not a positive value.</summary>
    [MessageLogging(EventId = 61009, Level = LogLevel.Error, Message = "Personal access token per-user limit ({maxTokens}) is not a positive value in the credential service configuration")]
    public static partial IGenericMessage MaxTokensInvalid(ILogger logger, int maxTokens);
}
