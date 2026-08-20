#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using Fdw.Configuration;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.EnvironmentVariable.Logging;

/// <summary>
/// Message logging for Environment Variable secret manager operations.
/// EventId range: 6001-6020
/// </summary>
[MessageLoggingTypeCode("ENVIRONMENTVARIA")]
public static partial class EnvironmentVariableLogger
{
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "Environment Variable configuration is null")]
    public static partial IGenericMessage ConfigurationNull(ILogger logger);

    [MessageLogging(EventId = 21001, Level = LogLevel.Error, Message = "Invalid configuration type. Expected {expectedType}, got {actualType}")]
    public static partial IGenericMessage InvalidConfigurationType(ILogger logger, string expectedType, string actualType);

    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Environment Variable secret manager creation failed: {errorMessage}")]
    public static partial IGenericMessage CreationFailed(ILogger logger, string errorMessage);

    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "Secret key cannot be null or empty for command type '{commandType}'")]
    public static partial IGenericMessage SecretKeyRequired(ILogger logger, string commandType);

    [MessageLogging(EventId = 21003, Level = LogLevel.Error, Message = "Unknown command type: {commandType}")]
    public static partial IGenericMessage UnknownCommandType(ILogger logger, string commandType);

    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "Failed to execute Environment Variable operation '{operation}' for secret '{secretKey}': {errorMessage}")]
    public static partial IGenericMessage OperationFailed(ILogger logger, string operation, string secretKey, string errorMessage);

    [MessageLogging(EventId = 21004, Level = LogLevel.Error, Message = "Command is null")]
    public static partial IGenericMessage CommandNull(ILogger logger);

    // Why: a secret that can't be fetched means the dependent connection can't be built — the
    // requested operation cannot complete. Error, not Warning.
    [MessageLogging(EventId = 31000, Level = LogLevel.Error, Message = "Environment variable '{variableName}' not found")]
    public static partial IGenericMessage EnvironmentVariableNotFound(ILogger logger, string variableName);

    // Why Debug, not Information: the secret manager is reconstructed on every health-check cycle, so this
    // is not a once-at-startup lifecycle record. (The reconstruction itself is tracked separately.)
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug, Message = "Environment Variable secret manager initialized with prefix '{prefix}'")]
    public static partial IGenericMessage ManagerInitialized(ILogger logger, string prefix);

    [MessageLogging(EventId = 11001, Level = LogLevel.Debug, Message = "Executing Environment Variable command '{commandType}' for secret '{secretKey}'")]
    public static partial IGenericMessage ExecutingCommand(ILogger logger, string commandType, string secretKey);

    [MessageLogging(EventId = 91002, Level = LogLevel.Error, Message = "Batch execution failed: {errorMessage}")]
    public static partial IGenericMessage BatchExecutionFailed(ILogger logger, string errorMessage);

    [MessageLogging(EventId = 41000, Level = LogLevel.Warning, Message = "SetSecret operation is not supported for environment variables. Use environment variable configuration instead.")]
    public static partial IGenericMessage SetSecretNotSupported(ILogger logger);

    [MessageLogging(EventId = 41001, Level = LogLevel.Warning, Message = "DeleteSecret operation is not supported for environment variables. Use environment variable configuration instead.")]
    public static partial IGenericMessage DeleteSecretNotSupported(ILogger logger);

    [MessageLogging(EventId = 11002, Level = LogLevel.Debug, Message = "Retrieved environment variable '{variableName}'")]
    public static partial IGenericMessage SecretRetrieved(ILogger logger, string variableName);

    [MessageLogging(EventId = 11003, Level = LogLevel.Debug, Message = "Listed {count} environment variables with prefix '{prefix}'")]
    public static partial IGenericMessage SecretsListed(ILogger logger, int count, string prefix);

    [MessageLogging(EventId = 11004, Level = LogLevel.Debug, Message = "Creating Environment Variable secret manager '{name}'")]
    public static partial IGenericMessage CreatingSecretManager(ILogger logger, string name);

    [MessageLogging(EventId = 11005, Level = LogLevel.Debug, Message = "Environment Variable secret manager '{name}' created successfully")]
    public static partial IGenericMessage SecretManagerCreated(ILogger logger, string name);

    [MessageLogging(EventId = 21005, Level = LogLevel.Error, Message = "Service is not of expected type '{expectedType}'")]
    public static partial IGenericMessage ServiceTypeMismatch(ILogger logger, string expectedType);

    [MessageLogging(EventId = 11006, Level = LogLevel.Debug, Message = "Executing typed command {commandType} for secret {secretKey}")]
    public static partial IGenericMessage ExecutingTypedCommand(ILogger logger, string commandType, string secretKey);

    [MessageLogging(EventId = 11007, Level = LogLevel.Debug, Message = "Executing batch of {count} commands")]
    public static partial IGenericMessage ExecutingBatch(ILogger logger, int count);

    // ═══════════════════════════════════════════════════════════════════════════
    // Trace Methods (6021-6025)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces entry into EnvironmentVariableSecretManagerFactory.CreateSecretManager.
    /// </summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace, Message = "Entering EnvironmentVariableSecretManagerFactory.CreateSecretManager")]
    public static partial IGenericMessage TraceCreateSecretManagerEntry(ILogger logger);

    /// <summary>
    /// Traces entry into EnvironmentVariableSecretManagerFactory.CreateSecretManager with generic configuration.
    /// </summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace, Message = "Entering EnvironmentVariableSecretManagerFactory.CreateSecretManager with IGenericConfiguration")]
    public static partial IGenericMessage TraceCreateSecretManagerGenericEntry(ILogger logger);

    /// <summary>
    /// Traces entry into EnvironmentVariableSecretManager.Execute.
    /// </summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace, Message = "Entering EnvironmentVariableSecretManager.Execute for command type '{commandType}'")]
    public static partial IGenericMessage TraceExecuteEntry(ILogger logger, string commandType);

    /// <summary>
    /// Traces secret key lookup for an environment variable.
    /// </summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace, Message = "Looking up environment variable for secret key '{secretKey}' with resolved name '{variableName}'")]
    public static partial IGenericMessage TraceSecretKeyLookup(ILogger logger, string secretKey, string variableName);

    /// <summary>
    /// Traces entry into EnvironmentVariableSecretManager.ExecuteBatch.
    /// </summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace, Message = "Entering EnvironmentVariableSecretManager.ExecuteBatch with {count} commands")]
    public static partial IGenericMessage TraceExecuteBatchEntry(ILogger logger, int count);
}
