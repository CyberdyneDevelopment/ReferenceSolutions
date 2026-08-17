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
/// MessageLogging for SQL-backed agent key operations.
/// EventId ranges in use: 11000-11009 informational, 21000-21009 trace/debug,
/// 31000-31009 rejections, 61000-61009 configuration faults, 71000-71009 database faults.
/// Key values are NEVER logged — only the key's id, which is not a credential.
/// </summary>
[MessageLoggingTypeCode("SQL")]
public static partial class AgentKeyLog
{
    /// <summary>Logged at Information level when an agent key is created.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information, Message = "Agent key '{label}' created for user '{userId}' (key '{keyId}')")]
    public static partial IGenericMessage KeyCreated(ILogger logger, Guid userId, Guid keyId, string label);

    /// <summary>Logged at Information level when an agent key is deleted.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information, Message = "Agent key '{keyId}' deleted for user '{userId}'")]
    public static partial IGenericMessage KeyDeleted(ILogger logger, Guid userId, Guid keyId);

    /// <summary>Logged at Warning level when an agent key is not found for the caller.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning, Message = "Agent key '{keyId}' not found for user '{userId}'")]
    public static partial IGenericMessage KeyNotFound(ILogger logger, Guid userId, Guid keyId);

    /// <summary>Logged at Error level when a database error occurs during an agent key operation.</summary>
    /// <summary>Logged at Warning level when an agent key validation fails.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning, Message = "Agent key validation failed")]
    public static partial IGenericMessage ValidationFailed(ILogger logger);

    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Database error during agent key operation '{operation}'")]
    public static partial IGenericMessage DatabaseError(ILogger logger, Exception ex, string operation);

    /// <summary>Logged at Error level when the vault name is not configured.</summary>
    [MessageLogging(EventId = 61000, Level = LogLevel.Error, Message = "Agent key credential vault name is missing from configuration")]
    public static partial IGenericMessage VaultNameMissing(ILogger logger);

    /// <summary>Logged at Error level when the vault cannot be resolved by name.</summary>
    [MessageLogging(EventId = 61001, Level = LogLevel.Error, Message = "Agent key credential vault '{vaultName}' could not be resolved")]
    public static partial IGenericMessage VaultResolveFailed(ILogger logger, string vaultName);

    /// <summary>Logs the start of an agent-key validation.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The message.</returns>
    /// <remarks>No parameter: the only input is the raw key, which is a credential.</remarks>
    [MessageLogging(EventId = 21000, Level = LogLevel.Trace, Message = "Validating an agent key")]
    public static partial IGenericMessage ValidatingKey(ILogger logger);

    /// <summary>Logs a key whose hash matched nothing.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning, Message = "Agent key rejected: no key matches that value")]
    public static partial IGenericMessage KeyNotRecognised(ILogger logger);

    /// <summary>Logs a key that exists but has been deactivated.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="keyId">The key's id.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 31003, Level = LogLevel.Warning, Message = "Agent key '{keyId}' rejected: the key is not active")]
    public static partial IGenericMessage KeyInactive(ILogger logger, Guid keyId);

    /// <summary>Logs a key that exists and is active but has expired.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="keyId">The key's id.</param>
    /// <param name="expiredAt">When it expired.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 31004, Level = LogLevel.Warning, Message = "Agent key '{keyId}' rejected: expired at {expiredAt:o}")]
    public static partial IGenericMessage KeyExpired(ILogger logger, Guid keyId, DateTimeOffset expiredAt);

    /// <summary>Logs a key that validated.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="keyId">The key's id.</param>
    /// <param name="userId">The owner.</param>
    /// <returns>The message.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information, Message = "Agent key '{keyId}' validated for user {userId}")]
    public static partial IGenericMessage KeyValidated(ILogger logger, Guid keyId, Guid userId);

    /// <summary>Logs a failure to record the key's last use.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="keyId">The key's id.</param>
    /// <returns>The message.</returns>
    /// <remarks>Deliberately not fatal: a valid key is still valid if the touch fails.</remarks>
    [MessageLogging(EventId = 21001, Level = LogLevel.Debug, Message = "Agent key '{keyId}' validated but its last-used timestamp was not recorded")]
    public static partial IGenericMessage LastUsedNotRecorded(ILogger logger, Guid keyId);
}
