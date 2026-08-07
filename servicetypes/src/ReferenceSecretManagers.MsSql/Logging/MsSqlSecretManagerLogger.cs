#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using Fdw.Configuration;
using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;
using Fdw.Services.SecretManagers.MsSql.Commands;
using Fdw.Services.SecretManagers.MsSql.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.MsSql.Logging;

/// <summary>
/// Message logging for MsSql secret manager operations.
/// EventId range: 6030-6065
/// </summary>
[MessageLoggingTypeCode("MSSQL2")]
public static partial class MsSqlSecretManagerLogger
{
    [MessageLogging(EventId = 21001, Level = LogLevel.Error, Message = "MsSql secret manager configuration is null")]
    public static partial IGenericMessage ConfigurationNull(ILogger logger);

    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "Invalid configuration type. Expected {expectedType}, got {actualType}")]
    public static partial IGenericMessage InvalidConfigurationType(ILogger logger, string expectedType, string actualType);

    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "MsSql secret manager creation failed: {errorMessage}")]
    public static partial IGenericMessage CreationFailed(ILogger logger, string errorMessage);

    [MessageLogging(EventId = 21003, Level = LogLevel.Error, Message = "Secret key cannot be null or empty for command type '{commandType}'")]
    public static partial IGenericMessage SecretKeyRequired(ILogger logger, string commandType);

    [MessageLogging(EventId = 21004, Level = LogLevel.Error, Message = "Unknown command type: {commandType}")]
    public static partial IGenericMessage UnknownCommandType(ILogger logger, string commandType);

    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "Failed to execute MsSql operation '{operation}' for secret '{secretKey}': {errorMessage}")]
    public static partial IGenericMessage OperationFailed(ILogger logger, string operation, string secretKey, string errorMessage);

    [MessageLogging(EventId = 21005, Level = LogLevel.Error, Message = "Command is null")]
    public static partial IGenericMessage CommandNull(ILogger logger);

    // Why: a secret that can't be fetched means the dependent connection can't be built — the
    // requested operation cannot complete. Error, not Warning.
    [MessageLogging(EventId = 31001, Level = LogLevel.Error, Message = "Secret '{secretKey}' not found in [{schema}].[{tableName}]")]
    public static partial IGenericMessage SecretNotFound(ILogger logger, string secretKey, string schema, string tableName);

    [MessageLogging(EventId = 11005, Level = LogLevel.Information, Message = "MsSql secret manager initialized for [{schema}].[{tableName}]")]
    public static partial IGenericMessage ManagerInitialized(ILogger logger, string schema, string tableName);

    [MessageLogging(EventId = 11006, Level = LogLevel.Debug, Message = "Executing MsSql command '{commandType}' for secret '{secretKey}'")]
    public static partial IGenericMessage ExecutingCommand(ILogger logger, string commandType, string secretKey);

    [MessageLogging(EventId = 71002, Level = LogLevel.Error, Message = "Batch execution failed: {errorMessage}")]
    public static partial IGenericMessage BatchExecutionFailed(ILogger logger, string errorMessage);

    [MessageLogging(EventId = 11007, Level = LogLevel.Debug, Message = "Retrieved secret '{secretKey}' from [{schema}].[{tableName}]")]
    public static partial IGenericMessage SecretRetrieved(ILogger logger, string secretKey, string schema, string tableName);

    [MessageLogging(EventId = 11008, Level = LogLevel.Debug, Message = "Listed {count} secrets from [{schema}].[{tableName}]")]
    public static partial IGenericMessage SecretsListed(ILogger logger, int count, string schema, string tableName);

    [MessageLogging(EventId = 11009, Level = LogLevel.Debug, Message = "Creating MsSql secret manager '{name}'")]
    public static partial IGenericMessage CreatingSecretManager(ILogger logger, string name);

    [MessageLogging(EventId = 11010, Level = LogLevel.Debug, Message = "MsSql secret manager '{name}' created successfully")]
    public static partial IGenericMessage SecretManagerCreated(ILogger logger, string name);

    [MessageLogging(EventId = 21006, Level = LogLevel.Error, Message = "Service is not of expected type '{expectedType}'")]
    public static partial IGenericMessage ServiceTypeMismatch(ILogger logger, string expectedType);

    [MessageLogging(EventId = 11011, Level = LogLevel.Debug, Message = "Executing typed command {commandType} for secret {secretKey}")]
    public static partial IGenericMessage ExecutingTypedCommand(ILogger logger, string commandType, string secretKey);

    [MessageLogging(EventId = 11012, Level = LogLevel.Debug, Message = "Executing batch of {count} commands")]
    public static partial IGenericMessage ExecutingBatch(ILogger logger, int count);

    [MessageLogging(EventId = 11013, Level = LogLevel.Information, Message = "Secret '{secretKey}' set in [{schema}].[{tableName}]")]
    public static partial IGenericMessage SecretSet(ILogger logger, string secretKey, string schema, string tableName);

    [MessageLogging(EventId = 11014, Level = LogLevel.Information, Message = "Secret '{secretKey}' deleted from [{schema}].[{tableName}]")]
    public static partial IGenericMessage SecretDeleted(ILogger logger, string secretKey, string schema, string tableName);

    [MessageLogging(EventId = 21007, Level = LogLevel.Error, Message = "Secret value is required for SetSecret command")]
    public static partial IGenericMessage SecretValueRequired(ILogger logger);

    [MessageLogging(EventId = 21008, Level = LogLevel.Error, Message = "Secret type is required for SetSecret command")]
    public static partial IGenericMessage SecretTypeRequired(ILogger logger);

    [MessageLogging(EventId = 71003, Level = LogLevel.Error, Message = "Failed to connect to MsSql secret store: {errorMessage}")]
    public static partial IGenericMessage ConnectionFailed(ILogger logger, Exception exception, string errorMessage);

    [MessageLogging(EventId = 11015, Level = LogLevel.Debug, Message = "Ensuring secrets schema [{schema}] exists")]
    public static partial IGenericMessage EnsuringSchema(ILogger logger, string schema);

    [MessageLogging(EventId = 11016, Level = LogLevel.Debug, Message = "Ensuring secrets table [{schema}].[{tableName}] exists")]
    public static partial IGenericMessage EnsuringTable(ILogger logger, string schema, string tableName);

    [MessageLogging(EventId = 11017, Level = LogLevel.Information, Message = "Created secrets schema [{schema}]")]
    public static partial IGenericMessage SchemaCreated(ILogger logger, string schema);

    [MessageLogging(EventId = 11018, Level = LogLevel.Information, Message = "Created secrets table [{schema}].[{tableName}]")]
    public static partial IGenericMessage TableCreated(ILogger logger, string schema, string tableName);

    [MessageLogging(EventId = 21009, Level = LogLevel.Error, Message = "Command type is required")]
    public static partial IGenericMessage CommandTypeRequired(ILogger logger);

    [MessageLogging(EventId = 71004, Level = LogLevel.Error, Message = "Failed to execute MsSql batch operation '{operation}' for secret '{secretKey}': {errorMessage}")]
    public static partial IGenericMessage BatchOperationException(ILogger logger, Exception exception, string operation, string secretKey, string errorMessage);

    [MessageLogging(EventId = 21010, Level = LogLevel.Error, Message = "Command must be of type {expectedType}")]
    public static partial IGenericMessage InvalidCommandType(ILogger logger, string expectedType);

    // ═══════════════════════════════════════════════════════════════════════════
    // Trace Methods (6059-6064)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces entry into MsSqlSecretManagerFactory.CreateSecretManager.
    /// </summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace, Message = "Entering MsSqlSecretManagerFactory.CreateSecretManager")]
    public static partial IGenericMessage TraceCreateSecretManagerEntry(ILogger logger);

    /// <summary>
    /// Traces entry into MsSqlSecretManagerFactory.CreateSecretManager with generic configuration.
    /// </summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Trace, Message = "Entering MsSqlSecretManagerFactory.CreateSecretManager with IGenericConfiguration")]
    public static partial IGenericMessage TraceCreateSecretManagerGenericEntry(ILogger logger);

    /// <summary>
    /// Traces entry into MsSqlSecretManager.Execute.
    /// </summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Trace, Message = "Entering MsSqlSecretManager.Execute for command type '{commandType}'")]
    public static partial IGenericMessage TraceExecuteEntry(ILogger logger, string commandType);

    /// <summary>
    /// Traces SQL query execution for a secret operation.
    /// </summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Trace, Message = "Executing SQL query for '{operation}' on [{schema}].[{tableName}]")]
    public static partial IGenericMessage TraceSqlQueryExecution(ILogger logger, string operation, string schema, string tableName);

    /// <summary>
    /// Traces entry into MsSqlSecretManager.ExecuteBatch.
    /// </summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Trace, Message = "Entering MsSqlSecretManager.ExecuteBatch with {count} commands")]
    public static partial IGenericMessage TraceExecuteBatchEntry(ILogger logger, int count);

    /// <summary>
    /// Traces entry into MsSqlSecretManager.ValidateCommand.
    /// </summary>
    [MessageLogging(EventId = 11024, Level = LogLevel.Trace, Message = "Entering MsSqlSecretManager.ValidateCommand for command type '{commandType}'")]
    public static partial IGenericMessage TraceValidateCommandEntry(ILogger logger, string commandType);
}
