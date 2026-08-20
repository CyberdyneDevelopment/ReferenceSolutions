#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;
using Fdw.Services.SecretManagers.Sqlite.Commands;
using Fdw.Services.SecretManagers.Sqlite.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.Sqlite.Logging;

/// <summary>
/// Message logging for SQLite secret manager operations.
/// TypeCode: SQLIT2
/// EventId ranges: 11005-11026 (Info/Debug/Trace), 21001-21010 (Error — validation/config),
///                 31001-31002 (Warning — recoverable), 71001-71005 (Error — exceptions),
///                 91000 (Critical — unrecoverable init)
/// </summary>
[MessageLoggingTypeCode("SQLIT2")]
public static partial class SqliteSecretManagerLogger
{
    // ─── Validation / configuration errors (21xxx) ──────────────────────────

    [MessageLogging(EventId = 21001, Level = LogLevel.Error, Message = "SQLite secret manager configuration is null")]
    public static partial IGenericMessage ConfigurationNull(ILogger logger);

    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "Invalid configuration type. Expected {expectedType}, got {actualType}")]
    public static partial IGenericMessage InvalidConfigurationType(ILogger logger, string expectedType, string actualType);

    [MessageLogging(EventId = 21003, Level = LogLevel.Error, Message = "Secret key cannot be null or empty for command type '{commandType}'")]
    public static partial IGenericMessage SecretKeyRequired(ILogger logger, string commandType);

    [MessageLogging(EventId = 21004, Level = LogLevel.Error, Message = "Unknown command type: {commandType}")]
    public static partial IGenericMessage UnknownCommandType(ILogger logger, string commandType);

    [MessageLogging(EventId = 21005, Level = LogLevel.Error, Message = "Command is null")]
    public static partial IGenericMessage CommandNull(ILogger logger);

    [MessageLogging(EventId = 21006, Level = LogLevel.Error, Message = "Service is not of expected type '{expectedType}'")]
    public static partial IGenericMessage ServiceTypeMismatch(ILogger logger, string expectedType);

    [MessageLogging(EventId = 21007, Level = LogLevel.Error, Message = "Secret value is required for SetSecret command")]
    public static partial IGenericMessage SecretValueRequired(ILogger logger);

    [MessageLogging(EventId = 21008, Level = LogLevel.Error, Message = "Secret type is required for SetSecret command")]
    public static partial IGenericMessage SecretTypeRequired(ILogger logger);

    [MessageLogging(EventId = 21009, Level = LogLevel.Error, Message = "Command type is required")]
    public static partial IGenericMessage CommandTypeRequired(ILogger logger);

    [MessageLogging(EventId = 21010, Level = LogLevel.Error, Message = "Command must be of type {expectedType}")]
    public static partial IGenericMessage InvalidCommandType(ILogger logger, string expectedType);

    // ─── Warnings — recoverable conditions (31xxx) ──────────────────────────

    // Why: a secret that can't be fetched means the dependent connection can't be built — the
    // requested operation cannot complete. Error, not Warning.
    [MessageLogging(EventId = 31001, Level = LogLevel.Error, Message = "Secret '{secretKey}' not found in table '{tableName}'")]
    public static partial IGenericMessage SecretNotFound(ILogger logger, string secretKey, string tableName);

    [MessageLogging(EventId = 31002, Level = LogLevel.Warning, Message = "No active row found for secret '{secretKey}' in table '{tableName}' — nothing to delete")]
    public static partial IGenericMessage SecretNotFoundOnDelete(ILogger logger, string secretKey, string tableName);

    // ─── Exception-bearing errors (71xxx) ───────────────────────────────────

    // Why: Exception param is ILogger, then Exception — stack trace and inner exceptions are preserved
    // for structured log sinks (e.g. Seq); errorMessage is the human-readable summary on the log line.

    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "Failed to execute SQLite operation '{operation}' for secret '{secretKey}': {errorMessage}")]
    public static partial IGenericMessage OperationFailed(ILogger logger, Exception exception, string operation, string secretKey, string errorMessage);

    [MessageLogging(EventId = 71002, Level = LogLevel.Error, Message = "Batch execution failed: {errorMessage}")]
    public static partial IGenericMessage BatchExecutionFailed(ILogger logger, string errorMessage);

    [MessageLogging(EventId = 71003, Level = LogLevel.Error, Message = "Failed to connect to SQLite secret store '{dataSource}': {errorMessage}")]
    public static partial IGenericMessage ConnectionFailed(ILogger logger, Exception exception, string dataSource, string errorMessage);

    [MessageLogging(EventId = 71004, Level = LogLevel.Error, Message = "Failed to execute SQLite batch operation '{operation}' for secret '{secretKey}': {errorMessage}")]
    public static partial IGenericMessage BatchOperationException(ILogger logger, Exception exception, string operation, string secretKey, string errorMessage);

    // ─── Critical — unrecoverable init (91xxx) ──────────────────────────────

    // Why: Critical level because a factory failure means the secret manager is completely
    // unusable — the app cannot resolve secrets at all until configuration is corrected.
    [MessageLogging(EventId = 91000, Level = LogLevel.Critical, Message = "SQLite secret manager creation failed: {errorMessage}")]
    public static partial IGenericMessage CreationFailed(ILogger logger, Exception exception, string errorMessage);

    // ─── Info — successful state transitions (11005-11016) ──────────────────

    // Why Debug, not Information: the secret manager is reconstructed on every health-check cycle, so this
    // is not a once-at-startup lifecycle record. (The reconstruction itself is tracked separately.)
    [MessageLogging(EventId = 11005, Level = LogLevel.Debug, Message = "SQLite secret manager initialized for table '{tableName}' in '{dataSource}'")]
    public static partial IGenericMessage ManagerInitialized(ILogger logger, string tableName, string dataSource);

    [MessageLogging(EventId = 11013, Level = LogLevel.Information, Message = "Secret '{secretKey}' set in table '{tableName}'")]
    public static partial IGenericMessage SecretSet(ILogger logger, string secretKey, string tableName);

    [MessageLogging(EventId = 11014, Level = LogLevel.Information, Message = "Secret '{secretKey}' deleted from table '{tableName}'")]
    public static partial IGenericMessage SecretDeleted(ILogger logger, string secretKey, string tableName);

    [MessageLogging(EventId = 11016, Level = LogLevel.Information, Message = "Created secrets table '{tableName}' in '{dataSource}'")]
    public static partial IGenericMessage TableCreated(ILogger logger, string tableName, string dataSource);

    // ─── Debug — diagnostic detail (11006-11015) ────────────────────────────

    [MessageLogging(EventId = 11006, Level = LogLevel.Debug, Message = "Executing SQLite command '{commandType}' for secret '{secretKey}'")]
    public static partial IGenericMessage ExecutingCommand(ILogger logger, string commandType, string secretKey);

    [MessageLogging(EventId = 11007, Level = LogLevel.Debug, Message = "Retrieved secret '{secretKey}' from table '{tableName}'")]
    public static partial IGenericMessage SecretRetrieved(ILogger logger, string secretKey, string tableName);

    [MessageLogging(EventId = 11008, Level = LogLevel.Debug, Message = "Listed {count} secrets from table '{tableName}'")]
    public static partial IGenericMessage SecretsListed(ILogger logger, int count, string tableName);

    [MessageLogging(EventId = 11009, Level = LogLevel.Debug, Message = "Creating SQLite secret manager '{name}'")]
    public static partial IGenericMessage CreatingSecretManager(ILogger logger, string name);

    [MessageLogging(EventId = 11010, Level = LogLevel.Debug, Message = "SQLite secret manager '{name}' created successfully")]
    public static partial IGenericMessage SecretManagerCreated(ILogger logger, string name);

    [MessageLogging(EventId = 11011, Level = LogLevel.Debug, Message = "Executing typed command {commandType} for secret {secretKey}")]
    public static partial IGenericMessage ExecutingTypedCommand(ILogger logger, string commandType, string secretKey);

    [MessageLogging(EventId = 11012, Level = LogLevel.Debug, Message = "Executing batch of {count} commands")]
    public static partial IGenericMessage ExecutingBatch(ILogger logger, int count);

    [MessageLogging(EventId = 11015, Level = LogLevel.Debug, Message = "Ensuring secrets table '{tableName}' exists in '{dataSource}'")]
    public static partial IGenericMessage EnsuringTable(ILogger logger, string tableName, string dataSource);

    // ─── Debug — version-on-write steps (11025-11026) ───────────────────────

    [MessageLogging(EventId = 11025, Level = LogLevel.Debug, Message = "Version-on-write: deactivated current row for secret '{secretKey}' in '{tableName}'")]
    public static partial IGenericMessage DebugVersionOnWriteDeactivate(ILogger logger, string secretKey, string tableName);

    [MessageLogging(EventId = 11026, Level = LogLevel.Debug, Message = "Version-on-write: inserted new row for secret '{secretKey}' in '{tableName}'")]
    public static partial IGenericMessage DebugVersionOnWriteInsert(ILogger logger, string secretKey, string tableName);

    // ─── Trace — method entry/exit and SQL execution (11017-11024) ──────────

    [MessageLogging(EventId = 11017, Level = LogLevel.Trace, Message = "Handler '{operation}' started for secret '{secretKey}' on table '{tableName}'")]
    public static partial IGenericMessage TraceHandlerEntry(ILogger logger, string operation, string secretKey, string tableName);

    [MessageLogging(EventId = 11018, Level = LogLevel.Trace, Message = "Handler '{operation}' completed: {descriptor}")]
    public static partial IGenericMessage TraceHandlerSuccess(ILogger logger, string operation, string descriptor);

    [MessageLogging(EventId = 11019, Level = LogLevel.Trace, Message = "Entering SqliteSecretManagerFactory.CreateSecretManager")]
    public static partial IGenericMessage TraceCreateSecretManagerEntry(ILogger logger);

    [MessageLogging(EventId = 11020, Level = LogLevel.Trace, Message = "Entering SqliteSecretManagerFactory.CreateSecretManager with IGenericConfiguration")]
    public static partial IGenericMessage TraceCreateSecretManagerGenericEntry(ILogger logger);

    [MessageLogging(EventId = 11021, Level = LogLevel.Trace, Message = "Entering SqliteSecretManager.Execute for command type '{commandType}'")]
    public static partial IGenericMessage TraceExecuteEntry(ILogger logger, string commandType);

    [MessageLogging(EventId = 11022, Level = LogLevel.Trace, Message = "Executing SQLite query for '{operation}' on table '{tableName}'")]
    public static partial IGenericMessage TraceQueryExecution(ILogger logger, string operation, string tableName);

    [MessageLogging(EventId = 11023, Level = LogLevel.Trace, Message = "Entering SqliteSecretManager.ExecuteBatch with {count} commands")]
    public static partial IGenericMessage TraceExecuteBatchEntry(ILogger logger, int count);

    [MessageLogging(EventId = 11024, Level = LogLevel.Trace, Message = "Entering SqliteSecretManager.ValidateCommand for command type '{commandType}'")]
    public static partial IGenericMessage TraceValidateCommandEntry(ILogger logger, string commandType);
}
