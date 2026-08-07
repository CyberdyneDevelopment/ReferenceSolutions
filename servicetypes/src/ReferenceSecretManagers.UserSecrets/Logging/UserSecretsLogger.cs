#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;
using Fdw.Services.SecretManagers.UserSecrets.Commands;
using Fdw.Services.SecretManagers.UserSecrets.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.UserSecrets.Logging;

/// <summary>
/// Message logging for User Secrets secret manager operations.
/// EventId range: 5101-5122
/// </summary>
[MessageLoggingTypeCode("USERSECRETS")]
public static partial class UserSecretsLogger
{
    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "User Secrets configuration is null")]
    public static partial IGenericMessage ConfigurationNull(ILogger logger);

    [MessageLogging(EventId = 21003, Level = LogLevel.Error, Message = "Invalid configuration type. Expected {expectedType}, got {actualType}")]
    public static partial IGenericMessage InvalidConfigurationType(ILogger logger, string expectedType, string actualType);

    [MessageLogging(EventId = 91002, Level = LogLevel.Error, Message = "User Secrets secret manager creation failed: {errorMessage}")]
    public static partial IGenericMessage CreationFailed(ILogger logger, string errorMessage);

    [MessageLogging(EventId = 21004, Level = LogLevel.Error, Message = "UserSecretsId is required when SecretsFilePath is not specified")]
    public static partial IGenericMessage UserSecretsIdRequired(ILogger logger);

    [MessageLogging(EventId = 31000, Level = LogLevel.Error, Message = "Secrets file not found at path: {filePath}")]
    public static partial IGenericMessage SecretsFileNotFound(ILogger logger, string filePath);

    [MessageLogging(EventId = 31001, Level = LogLevel.Warning, Message = "Secrets file not found at path '{filePath}', but configuration is optional")]
    public static partial IGenericMessage SecretsFileNotFoundOptional(ILogger logger, string filePath);

    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Failed to load secrets from '{filePath}': {errorMessage}")]
    public static partial IGenericMessage SecretsLoadFailed(ILogger logger, string filePath, string errorMessage);

    [MessageLogging(EventId = 11002, Level = LogLevel.Information, Message = "Loaded {count} secrets from '{filePath}'")]
    public static partial IGenericMessage SecretsLoaded(ILogger logger, int count, string filePath);

    [MessageLogging(EventId = 21005, Level = LogLevel.Error, Message = "Secret key cannot be null or empty for command type '{commandType}'")]
    public static partial IGenericMessage SecretKeyRequired(ILogger logger, string commandType);

    [MessageLogging(EventId = 21006, Level = LogLevel.Error, Message = "Unknown command type: {commandType}")]
    public static partial IGenericMessage UnknownCommandType(ILogger logger, string commandType);

    [MessageLogging(EventId = 91003, Level = LogLevel.Error, Message = "Failed to execute User Secrets operation '{operation}' for secret '{secretKey}': {errorMessage}")]
    public static partial IGenericMessage OperationFailed(ILogger logger, string operation, string secretKey, string errorMessage);

    [MessageLogging(EventId = 41000, Level = LogLevel.Error, Message = "Secrets have not been loaded")]
    public static partial IGenericMessage SecretsNotLoaded(ILogger logger);

    [MessageLogging(EventId = 21007, Level = LogLevel.Error, Message = "Command is null")]
    public static partial IGenericMessage CommandNull(ILogger logger);

    [MessageLogging(EventId = 41001, Level = LogLevel.Error, Message = "Operation '{operation}' is not supported by User Secrets (read-only)")]
    public static partial IGenericMessage OperationNotSupported(ILogger logger, string operation);

    [MessageLogging(EventId = 31002, Level = LogLevel.Error, Message = "Secret '{secretKey}' not found")]
    public static partial IGenericMessage SecretNotFound(ILogger logger, string secretKey);

    [MessageLogging(EventId = 11003, Level = LogLevel.Debug, Message = "File watcher enabled for '{filePath}'")]
    public static partial IGenericMessage FileWatcherEnabled(ILogger logger, string filePath);

    [MessageLogging(EventId = 91004, Level = LogLevel.Warning, Message = "Failed to set up file watcher: {errorMessage}")]
    public static partial IGenericMessage FileWatcherFailed(ILogger logger, string errorMessage);

    [MessageLogging(EventId = 91005, Level = LogLevel.Error, Message = "Batch execution failed: {errorMessage}")]
    public static partial IGenericMessage BatchExecutionFailed(ILogger logger, string errorMessage);

    [MessageLogging(EventId = 11004, Level = LogLevel.Debug, Message = "Executing command {commandType} for secret {secretKey}")]
    public static partial IGenericMessage ExecutingCommand(ILogger logger, string commandType, string secretKey);

    [MessageLogging(EventId = 11005, Level = LogLevel.Debug, Message = "Executing typed command {commandType} for secret {secretKey}")]
    public static partial IGenericMessage ExecutingTypedCommand(ILogger logger, string commandType, string secretKey);

    [MessageLogging(EventId = 11006, Level = LogLevel.Debug, Message = "Executing batch of {count} commands")]
    public static partial IGenericMessage ExecutingBatch(ILogger logger, int count);

    [MessageLogging(EventId = 11007, Level = LogLevel.Debug, Message = "Secrets file changed, reloading...")]
    public static partial IGenericMessage SecretsFileChanged(ILogger logger);
}
