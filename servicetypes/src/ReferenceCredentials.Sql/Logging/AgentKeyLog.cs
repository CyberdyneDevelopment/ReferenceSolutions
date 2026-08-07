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
/// EventId range: 7960-7979. Key values are never logged.
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
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Database error during agent key operation '{operation}'")]
    public static partial IGenericMessage DatabaseError(ILogger logger, Exception ex, string operation);

    /// <summary>Logged at Error level when the vault name is not configured.</summary>
    [MessageLogging(EventId = 61000, Level = LogLevel.Error, Message = "Agent key credential vault name is missing from configuration")]
    public static partial IGenericMessage VaultNameMissing(ILogger logger);

    /// <summary>Logged at Error level when the vault cannot be resolved by name.</summary>
    [MessageLogging(EventId = 61001, Level = LogLevel.Error, Message = "Agent key credential vault '{vaultName}' could not be resolved")]
    public static partial IGenericMessage VaultResolveFailed(ILogger logger, string vaultName);
}
