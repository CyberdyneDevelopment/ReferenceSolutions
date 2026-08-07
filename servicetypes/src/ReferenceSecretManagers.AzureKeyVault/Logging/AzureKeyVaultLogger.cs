#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;
using ReferenceSecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.AzureKeyVault.Logging;

/// <summary>
/// Message logging for Azure Key Vault secret manager operations.
/// EventId range: 5001-5023
/// </summary>
[MessageLoggingTypeCode("AZUREKEYVAULT")]
public static partial class AzureKeyVaultLogger
{
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "Azure Key Vault configuration is null")]
    public static partial IGenericMessage ConfigurationNull(ILogger logger);

    [MessageLogging(EventId = 21001, Level = LogLevel.Error, Message = "Invalid configuration type. Expected {expectedType}, got {actualType}")]
    public static partial IGenericMessage InvalidConfigurationType(ILogger logger, string expectedType, string actualType);

    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Azure Key Vault secret manager creation failed: {errorMessage}")]
    public static partial IGenericMessage CreationFailed(ILogger logger, string errorMessage);

    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "Azure Key Vault URI is null or empty")]
    public static partial IGenericMessage VaultUriNullOrEmpty(ILogger logger);

    [MessageLogging(EventId = 61000, Level = LogLevel.Error, Message = "Authentication method '{authMethod}' is not supported")]
    public static partial IGenericMessage UnsupportedAuthenticationMethod(ILogger logger, string authMethod);

    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Failed to initialize Azure Key Vault client: {errorMessage}")]
    public static partial IGenericMessage ClientInitializationFailed(ILogger logger, string errorMessage);

    [MessageLogging(EventId = 21003, Level = LogLevel.Error, Message = "Secret key cannot be null or empty for command type '{commandType}'")]
    public static partial IGenericMessage SecretKeyRequired(ILogger logger, string commandType);

    [MessageLogging(EventId = 61001, Level = LogLevel.Error, Message = "Unknown command type: {commandType}")]
    public static partial IGenericMessage UnknownCommandType(ILogger logger, string commandType);

    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "Failed to execute Azure Key Vault operation '{operation}' for secret '{secretKey}': {errorMessage}")]
    public static partial IGenericMessage OperationFailed(ILogger logger, string operation, string secretKey, string errorMessage);

    [MessageLogging(EventId = 61002, Level = LogLevel.Error, Message = "Azure Key Vault client is not initialized")]
    public static partial IGenericMessage ClientNotInitialized(ILogger logger);

    [MessageLogging(EventId = 21004, Level = LogLevel.Error, Message = "Command is null")]
    public static partial IGenericMessage CommandNull(ILogger logger);

    [MessageLogging(EventId = 21005, Level = LogLevel.Error, Message = "Secret value parameter is required for SetSecret operation")]
    public static partial IGenericMessage SecretValueRequired(ILogger logger);

    [MessageLogging(EventId = 71002, Level = LogLevel.Warning, Message = "Azure Key Vault accessibility check failed: {errorMessage}")]
    public static partial IGenericMessage AccessibilityCheckFailed(ILogger logger, string errorMessage);

    [MessageLogging(EventId = 11000, Level = LogLevel.Information, Message = "Azure Key Vault client initialized successfully with authentication method '{authMethod}'")]
    public static partial IGenericMessage ClientInitialized(ILogger logger, string authMethod);

    [MessageLogging(EventId = 11001, Level = LogLevel.Debug, Message = "Executing Azure Key Vault command '{commandType}' for secret '{secretKey}'")]
    public static partial IGenericMessage ExecutingCommand(ILogger logger, string commandType, string secretKey);

    [MessageLogging(EventId = 71003, Level = LogLevel.Error, Message = "Batch execution failed: {errorMessage}")]
    public static partial IGenericMessage BatchExecutionFailed(ILogger logger, string errorMessage);

    [MessageLogging(EventId = 71004, Level = LogLevel.Error, Message = "Failed to check Azure Key Vault accessibility: {errorMessage}")]
    public static partial IGenericMessage AccessibilityCheckException(ILogger logger, string errorMessage);

    [MessageLogging(EventId = 61003, Level = LogLevel.Error, Message = "Azure Key Vault certificate client is not initialized")]
    public static partial IGenericMessage CertificateClientNotInitialized(ILogger logger);

    [MessageLogging(EventId = 11002, Level = LogLevel.Information, Message = "Certificate client initialized for vault")]
    public static partial IGenericMessage CertificateClientInitialized(ILogger logger);

    [MessageLogging(EventId = 11003, Level = LogLevel.Debug, Message = "Executing command via handler '{handlerType}' for '{secretKey}'")]
    public static partial IGenericMessage ExecutingViaHandler(ILogger logger, string handlerType, string secretKey);

    [MessageLogging(EventId = 61004, Level = LogLevel.Error, Message = "No handler found for command type '{commandType}'")]
    public static partial IGenericMessage NoHandlerFound(ILogger logger, string commandType);

    [MessageLogging(EventId = 11004, Level = LogLevel.Debug, Message = "Executing typed command {commandType} for secret {secretKey}")]
    public static partial IGenericMessage ExecutingTypedCommand(ILogger logger, string commandType, string secretKey);

    [MessageLogging(EventId = 11005, Level = LogLevel.Debug, Message = "Executing batch of {count} commands")]
    public static partial IGenericMessage ExecutingBatch(ILogger logger, int count);
}
